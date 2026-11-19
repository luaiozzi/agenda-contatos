namespace Agenda.Api.Exceptions;

/// <summary>E-mail ou senha incorretos (HTTP 401).</summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
