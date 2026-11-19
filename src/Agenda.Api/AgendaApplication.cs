using Agenda.Api.Data;
using Agenda.Api.Middleware;
using Agenda.Api.Repositories;
using Agenda.Api.Services;
using Agenda.Api.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

        // Persistência (Entity Framework Core + SQLite)
        builder.Services.AddDbContext<AgendaDbContext>(options => options.UseSqlite(connectionString));

        // Bibliotecas acessórias
        builder.Services.AddValidatorsFromAssemblyContaining<ContactRequestValidator>();

        // Injeção de dependência: repositório e serviço
        builder.Services.AddScoped<IContactRepository, ContactRepository>();
        builder.Services.AddScoped<IContactService, ContactService>();

        builder.Services.AddControllers();

        // CORS para o frontend em desenvolvimento (Vite)
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy => policy
                .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Agenda API",
                Version = "v1",
                Description = "CRUD de contatos (nome, e-mail e telefone)."
            });

            var xmlFile = $"{typeof(AgendaApplication).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        var app = builder.Build();

        // Cria o banco (SQLite) automaticamente na primeira execução.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();
            db.Database.EnsureCreated();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Agenda API v1");
        });

        app.UseCors("Frontend");

        app.MapControllers();

        return app;
    }
}
