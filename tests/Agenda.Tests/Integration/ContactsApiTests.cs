using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Agenda.Api.Dtos;
using Agenda.Api.Models;
using FluentAssertions;

namespace Agenda.Tests.Integration;

public class ContactsApiTests : IClassFixture<ApiServer>
{
    private readonly HttpClient _client;

    public ContactsApiTests(ApiServer server)
    {
        _client = server.Client;
    }

    private static object NewPayload(string name = "Ana Souza", string? email = null, string phone = "(19) 99999-8888")
    {
        return new
        {
            name,
            email = email ?? $"{Guid.NewGuid():N}@example.com",
            phone
        };
    }

    private async Task<ContactDto> CreateContactAsync(object? payload = null)
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", payload ?? NewPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<ContactDto>();
        dto.Should().NotBeNull();
        return dto!;
    }

    [Fact]
    public async Task Create_WithValidData_Returns201AndNormalizedContact()
    {
        var email = $"{Guid.NewGuid():N}@Example.com";

        var response = await _client.PostAsJsonAsync("/api/contacts", NewPayload(email: email));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<ContactDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Email.Should().Be(email.ToLowerInvariant());
        dto.Phone.Should().Be("19999998888");
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/contacts",
            new { name = "", email = "invalido", phone = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        errors.TryGetProperty("name", out _).Should().BeTrue();
        errors.TryGetProperty("email", out _).Should().BeTrue();
        errors.TryGetProperty("phone", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_Returns409()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await CreateContactAsync(NewPayload(email: email));

        var response = await _client.PostAsJsonAsync("/api/contacts", NewPayload(name: "Outra Pessoa", email: email.ToUpperInvariant()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/contacts/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsContact()
    {
        var created = await CreateContactAsync();

        var response = await _client.GetAsync($"/api/contacts/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ContactDto>();
        dto!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Update_ThenDelete_CompletesFullLifecycle()
    {
        var created = await CreateContactAsync();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/contacts/{created.Id}",
            new { name = "Nome Atualizado", email = created.Email, phone = "(11) 3333-4444" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ContactDto>();
        updated!.Name.Should().Be("Nome Atualizado");
        updated.Phone.Should().Be("1133334444");
        updated.UpdatedAt.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/api/contacts/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/contacts/999999", NewPayload());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var response = await _client.DeleteAsync("/api/contacts/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithSearch_ReturnsOnlyMatchingContacts()
    {
        var token = Guid.NewGuid().ToString("N").Substring(0, 10);

        await CreateContactAsync(NewPayload(name: $"Zed {token}"));
        await CreateContactAsync(NewPayload(name: $"Zoe {token}"));
        await CreateContactAsync(NewPayload(name: "Pessoa Sem Relacao"));

        var response = await _client.GetAsync($"/api/contacts?search={token}&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ContactDto>>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(2);
        result.Items.Select(c => c.Name).Should().Equal($"Zed {token}", $"Zoe {token}");
    }

    [Fact]
    public async Task Swagger_DocumentIsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
