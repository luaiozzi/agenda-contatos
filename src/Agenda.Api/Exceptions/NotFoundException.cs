namespace Agenda.Api.Exceptions;

/// <summary>Recurso não encontrado (HTTP 404).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
