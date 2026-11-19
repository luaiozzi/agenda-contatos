using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Agenda.Api.Models;
using Agenda.Api.Services;
using Agenda.Api.Settings;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace Agenda.Tests.Services;

public class JwtTokenServiceTests
{
    private static readonly JwtSettings Jwt = new()
    {
        Key = "chave-de-testes-com-mais-de-32-caracteres-0123456789",
        Issuer = "AgendaApi.Tests",
        Audience = "AgendaClient.Tests",
        ExpirationMinutes = 30
    };

    private static readonly AppUser User = new() { Id = 7, Email = "ana@example.com" };

    private static TokenValidationParameters ValidationParameters(string key) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = Jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    [Fact]
    public void Generate_ReturnsTokenWithIssuerAudienceAndClaims()
    {
        var result = new JwtTokenService(Jwt).Generate(User);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        token.Issuer.Should().Be(Jwt.Issuer);
        token.Audiences.Should().Contain(Jwt.Audience);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "7");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "ana@example.com");
    }

    [Fact]
    public void Generate_SetsExpirationFromSettings()
    {
        var before = DateTime.UtcNow;

        var result = new JwtTokenService(Jwt).Generate(User);

        result.ExpiresAt.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(5));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.ValidTo.Should().BeCloseTo(result.ExpiresAt, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Generate_ProducesTokenThatValidatesWithTheSameKey()
    {
        var result = new JwtTokenService(Jwt).Generate(User);

        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(result.Token, ValidationParameters(Jwt.Key), out var validated);

        principal.Identity!.IsAuthenticated.Should().BeTrue();
        validated.Should().NotBeNull();
    }

    [Fact]
    public void Generate_ProducesTokenThatIsRejectedWithAnotherKey()
    {
        var result = new JwtTokenService(Jwt).Generate(User);
        var handler = new JwtSecurityTokenHandler();
        var parameters = ValidationParameters("outra-chave-totalmente-diferente-com-32-caracteres!!");

        Action act = () => handler.ValidateToken(result.Token, parameters, out _);

        act.Should().Throw<SecurityTokenException>();
    }
}
