using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Agenda.Api.Dtos;
using FluentAssertions;

namespace Agenda.Tests.Integration;

public class AuthApiTests : IClassFixture<ApiServer>
{
    private readonly ApiServer _server;

    public AuthApiTests(ApiServer server)
    {
        _server = server;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _server.AnonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = ApiServer.AdminEmail, password = ApiServer.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be(ApiServer.AdminEmail);
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_IgnoresEmailCase()
    {
        var response = await _server.AnonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "ADMIN@AGENDA.COM", password = ApiServer.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _server.AnonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = ApiServer.AdminEmail, password = "senha-errada" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _server.AnonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "ninguem@agenda.com", password = ApiServer.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyFields_Returns400WithFieldErrors()
    {
        var response = await _server.AnonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        errors.TryGetProperty("email", out _).Should().BeTrue();
        errors.TryGetProperty("password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Contacts_WithoutToken_Returns401()
    {
        var response = await _server.AnonymousClient.GetAsync("/api/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Contacts_WithInvalidToken_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/contacts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalido.aqui");

        var response = await _server.AnonymousClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Contacts_WithValidToken_Returns200()
    {
        var response = await _server.Client.GetAsync("/api/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
