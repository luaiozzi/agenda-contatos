using Agenda.Api.Dtos;
using Agenda.Api.Exceptions;
using Agenda.Api.Models;
using Agenda.Api.Repositories;
using Agenda.Api.Services;
using Agenda.Api.Settings;
using Agenda.Api.Validators;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Agenda.Tests.Services;

public class AuthServiceTests
{
    private static readonly JwtSettings Jwt = new()
    {
        Key = "chave-de-testes-com-mais-de-32-caracteres-0123456789",
        Issuer = "AgendaApi.Tests",
        Audience = "AgendaClient.Tests",
        ExpirationMinutes = 30
    };

    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(
            _users.Object,
            _hasher,
            new JwtTokenService(Jwt),
            new LoginRequestValidator());
    }

    private AppUser CreateUser(string email = "admin@agenda.com", string password = "Admin@123")
    {
        var user = new AppUser { Id = 1, Email = email, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = CreateUser();
        _users
            .Setup(r => r.GetByEmailAsync("admin@agenda.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(
            new LoginRequest { Email = "admin@agenda.com", Password = "Admin@123" },
            CancellationToken.None);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Email.Should().Be("admin@agenda.com");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmailBeforeSearching()
    {
        var user = CreateUser();
        _users
            .Setup(r => r.GetByEmailAsync("admin@agenda.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(
            new LoginRequest { Email = "  Admin@Agenda.COM ", Password = "Admin@123" },
            CancellationToken.None);

        result.Email.Should().Be("admin@agenda.com");
        _users.Verify(
            r => r.GetByEmailAsync("admin@agenda.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentials()
    {
        _users
            .Setup(r => r.GetByEmailAsync("admin@agenda.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        Func<Task> act = () => _service.LoginAsync(
            new LoginRequest { Email = "admin@agenda.com", Password = "senha-errada" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsInvalidCredentials()
    {
        _users
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        Func<Task> act = () => _service.LoginAsync(
            new LoginRequest { Email = "ninguem@agenda.com", Password = "Admin@123" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidRequest_ThrowsValidationAndDoesNotQuery()
    {
        Func<Task> act = () => _service.LoginAsync(
            new LoginRequest { Email = "", Password = "" },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _users.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
