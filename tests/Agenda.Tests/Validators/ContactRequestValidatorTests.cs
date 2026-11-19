using Agenda.Api.Dtos;
using Agenda.Api.Validators;
using FluentAssertions;

namespace Agenda.Tests.Validators;

public class ContactRequestValidatorTests
{
    private readonly ContactRequestValidator _validator = new();

    private static ContactRequest ValidRequest() => new()
    {
        Name = "Ana Souza",
        Email = "ana.souza@example.com",
        Phone = "(19) 99999-8888"
    };

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Validate_WithInvalidName_ReturnsNameError(string? name)
    {
        var request = ValidRequest();
        request.Name = name;

        var result = _validator.Validate(request);

        result.Errors.Select(e => e.PropertyName).Should().Contain("Name");
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsNameError()
    {
        var request = ValidRequest();
        request.Name = new string('a', 101);

        var result = _validator.Validate(request);

        result.Errors.Select(e => e.PropertyName).Should().Contain("Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem-arroba.com")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana@example")]
    [InlineData("ana souza@example.com")]
    public void Validate_WithInvalidEmail_ReturnsEmailError(string? email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _validator.Validate(request);

        result.Errors.Select(e => e.PropertyName).Should().Contain("Email");
    }

    [Fact]
    public void Validate_WithEmailSurroundedBySpaces_IsValid()
    {
        var request = ValidRequest();
        request.Email = "  ana@example.com  ";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("(19) 89999-8888")]
    public void Validate_WithInvalidPhone_ReturnsPhoneError(string? phone)
    {
        var request = ValidRequest();
        request.Phone = phone;

        var result = _validator.Validate(request);

        result.Errors.Select(e => e.PropertyName).Should().Contain("Phone");
    }
}
