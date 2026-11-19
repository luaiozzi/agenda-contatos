namespace Agenda.Api.Settings;

/// <summary>
/// Usuário administrador criado automaticamente na primeira execução (seção "Seed").
/// Se algum dos dois campos estiver vazio, nenhum usuário é criado.
/// </summary>
public class SeedSettings
{
    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;
}
