namespace Agenda.Api.Dtos;

/// <summary>
/// Dados enviados para criar ou atualizar um contato.
/// As propriedades são anuláveis de propósito: a validação fica a cargo do FluentValidation,
/// o que mantém um único formato de resposta de erro (400 com lista de campos).
/// </summary>
public class ContactRequest
{
    /// <example>Ana Souza</example>
    public string? Name { get; set; }

    /// <example>ana.souza@example.com</example>
    public string? Email { get; set; }

    /// <summary>Aceita máscara; ex.: (19) 99999-8888, +55 19 99999-8888 ou 19999998888.</summary>
    /// <example>(19) 99999-8888</example>
    public string? Phone { get; set; }
}
