namespace Agenda.Api.Settings;

/// <summary>Configuração do token JWT (seção "Jwt" do appsettings / variáveis de ambiente).</summary>
public class JwtSettings
{
    public const int MinimumKeyLength = 32;

    /// <summary>Segredo usado para assinar os tokens (HMAC-SHA256). Mínimo de 32 caracteres.</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "AgendaApi";

    public string Audience { get; set; } = "AgendaClient";

    public int ExpirationMinutes { get; set; } = 60;
}
