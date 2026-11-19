using System.Text;
using Agenda.Api.Data;
using Agenda.Api.Middleware;
using Agenda.Api.Models;
using Agenda.Api.Repositories;
using Agenda.Api.Services;
using Agenda.Api.Settings;
using Agenda.Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Agenda.Api;

/// <summary>
/// Monta a aplicação web. Fica separado do Program.cs para que os testes de integração
/// consigam subir a API real (Kestrel) com uma configuração própria.
/// </summary>
public static class AgendaApplication
{
    public static WebApplication Build(string[] args)
    {
        // ApplicationName explícito: o MVC usa esse nome para encontrar os controllers,
        // e em um processo de teste o "assembly de entrada" não é a API.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(AgendaApplication).Assembly.GetName().Name
        });

        var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=agenda.db";

        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < JwtSettings.MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"A configuração 'Jwt:Key' é obrigatória e deve ter pelo menos {JwtSettings.MinimumKeyLength} caracteres.");
        }

        var seedSettings = builder.Configuration.GetSection("Seed").Get<SeedSettings>() ?? new SeedSettings();

        // Persistência (Entity Framework Core + SQLite)
        builder.Services.AddDbContext<AgendaDbContext>(options => options.UseSqlite(connectionString));

        // Bibliotecas acessórias
        builder.Services.AddValidatorsFromAssemblyContaining<ContactRequestValidator>();

        // Injeção de dependência: configuração, repositórios e serviços
        builder.Services.AddSingleton(jwtSettings);
        builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        builder.Services.AddSingleton<ITokenService, JwtTokenService>();

        builder.Services.AddScoped<IContactRepository, ContactRepository>();
        builder.Services.AddScoped<IContactService, ContactService>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IAuthService, AuthService>();

        // Autenticação JWT
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        builder.Services.AddAuthorization();

        builder.Services.AddControllers();

        // CORS para o frontend em desenvolvimento (Vite)
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy => policy
                .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        // Swagger (com botão "Authorize" para informar o token JWT)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Agenda API",
                Version = "v1",
                Description = "CRUD de contatos (nome, e-mail e telefone) com autenticação JWT."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe o token JWT obtido em POST /api/auth/login (apenas o token, sem a palavra Bearer)."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{typeof(AgendaApplication).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        var app = builder.Build();

        // Cria o banco (SQLite) e o usuário administrador na primeira execução.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();
            db.Database.EnsureCreated();

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
            DbSeeder.SeedAdmin(db, hasher, seedSettings);
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Agenda API v1");
        });

        app.UseCors("Frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
