using Agenda.Api.Helpers;
using FluentAssertions;

namespace Agenda.Tests.Helpers;

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("(19) 99999-8888", "19999998888")]
    [InlineData("19 3333-4444", "1933334444")]
    [InlineData("+55 (19) 99999-8888", "19999998888")]
    [InlineData("5519999998888", "19999998888")]
    [InlineData("551933334444", "1933334444")]
    [InlineData("19999998888", "19999998888")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_RemovesMaskAndCountryCode(string? input, string expected)
    {
        PhoneNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("(19) 99999-8888")]
    [InlineData("(11) 91234-5678")]
    [InlineData("(19) 3333-4444")]
    [InlineData("(55) 99999-8888")]
    [InlineData("+55 19 99999-8888")]
    public void IsValid_WithValidNumbers_ReturnsTrue(string input)
    {
        PhoneNormalizer.IsValid(input).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("(19) 89999-8888")]
    [InlineData("(19) 6333-4444")]
    [InlineData("(09) 99999-8888")]
    [InlineData("(10) 99999-8888")]
    [InlineData("199999988889")]
    [InlineData("abcdefghijk")]
    public void IsValid_WithInvalidNumbers_ReturnsFalse(string? input)
    {
        PhoneNormalizer.IsValid(input).Should().BeFalse();
    }
}
