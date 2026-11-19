namespace Agenda.Api.Helpers;

/// <summary>
/// Normalização e validação de telefones brasileiros.
/// Formato armazenado: somente dígitos, DDD + número (10 ou 11 dígitos).
/// </summary>
public static class PhoneNormalizer
{
    public static string OnlyDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return new string(value.Where(c => c >= '0' && c <= '9').ToArray());
    }

    /// <summary>
    /// Remove máscara e o código do país (55), quando presente.
    /// </summary>
    public static string Normalize(string? value)
    {
        var digits = OnlyDigits(value);

        // 55 + DDD + número (10 ou 11 dígitos) = 12 ou 13 dígitos
        if ((digits.Length == 12 || digits.Length == 13) && digits.StartsWith("55", StringComparison.Ordinal))
        {
            digits = digits.Substring(2);
        }

        return digits;
    }

    /// <summary>
    /// Regras:
    /// - 11 dígitos: celular, o número começa com 9 (ex.: 19 99999-8888);
    /// - 10 dígitos: fixo, o número começa com 2 a 5 (ex.: 19 3333-4444);
    /// - DDD de dois dígitos, sem zero à esquerda e sem terminar em zero.
    /// </summary>
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);

        if (digits.Length != 10 && digits.Length != 11)
        {
            return false;
        }

        if (digits[0] == '0' || digits[1] == '0')
        {
            return false;
        }

        if (digits.Length == 11)
        {
            return digits[2] == '9';
        }

        return digits[2] >= '2' && digits[2] <= '5';
    }
}
