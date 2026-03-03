using ClipboardManager.Helpers;

namespace ClipboardManager.Tests;

public class TextTransformHelperTests
{
    [Theory]
    [InlineData("hello world", "HELLO WORLD")]
    [InlineData("",            ""             )]
    public void ToUpperCase_Correct(string input, string expected)
        => Assert.Equal(expected, TextTransformHelper.ToUpperCase(input));

    [Theory]
    [InlineData("HELLO WORLD", "hello world")]
    public void ToLowerCase_Correct(string input, string expected)
        => Assert.Equal(expected, TextTransformHelper.ToLowerCase(input));

    [Fact]
    public void ToTitleCase_Correct()
        => Assert.Equal("Hello World", TextTransformHelper.ToTitleCase("hello world"));

    [Fact]
    public void ToSentenceCase_CapitalisesFirstChar()
    {
        var result = TextTransformHelper.ToSentenceCase("hello world");
        Assert.StartsWith("H", result);
    }

    [Fact]
    public void TrimWhitespace_RemovesLeadingTrailing()
        => Assert.Equal("hi", TextTransformHelper.TrimWhitespace("  hi  "));

    [Fact]
    public void RemoveExtraSpaces_CollapsesInternalSpaces()
        => Assert.Equal("a b c", TextTransformHelper.RemoveExtraSpaces("a   b   c"));

    [Fact]
    public void Base64_RoundTrip()
    {
        var original = "Hello, Base64!";
        var encoded  = TextTransformHelper.EncodeBase64(original);
        var decoded  = TextTransformHelper.DecodeBase64(encoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void DecodeBase64_InvalidInput_ReturnsErrorString()
        => Assert.Equal("[Invalid Base64]", TextTransformHelper.DecodeBase64("not valid!!!"));

    [Fact]
    public void Url_RoundTrip()
    {
        var original = "hello world & more=1";
        var encoded  = TextTransformHelper.UrlEncode(original);
        var decoded  = TextTransformHelper.UrlDecode(encoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Html_RoundTrip()
    {
        var original = "<div class=\"x\">&amp;</div>";
        var encoded  = TextTransformHelper.HtmlEncode(original);
        var decoded  = TextTransformHelper.HtmlDecode(encoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void ReverseText_Correct()
        => Assert.Equal("olleh", TextTransformHelper.ReverseText("hello"));

    [Fact]
    public void CountCharacters_ReturnsExpectedFormat()
    {
        var result = TextTransformHelper.CountCharacters("hello world");
        Assert.Contains("Characters:", result);
        Assert.Contains("Words:",      result);
        Assert.Contains("Lines:",      result);
    }
}
