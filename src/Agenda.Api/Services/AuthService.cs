using Agenda.Api.Dtos;
using Agenda.Api.Exceptions;
using Agenda.Api.Models;
using Agenda.Api.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Agenda.Api.Services;

/// <summary>
/// Autenticação por e-mail e senha.
/// E-mail desconhecido e senha errada devolvem a MESMA mensagem, para não revelar quais e-mails existem.
/// </summary>
public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "E-mail ou senha inválidos.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly ITokenService _tokens;
    private readonly IValidator<LoginRequest> _validator;

    public AuthService(
        IUserRepository users,
        IPasswordHasher<AppUser> hasher,
        ITokenService tokens,
        IValidator<LoginRequest> validator)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _validator = validator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email!.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!) == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        var token = _tokens.Generate(user);

        return new LoginResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            Email = user.Email
        };
    }
}
