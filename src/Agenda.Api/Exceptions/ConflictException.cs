namespace Agenda.Api.Exceptions;

/// <summary>Violação de regra de negócio que conflita com o estado atual (HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
