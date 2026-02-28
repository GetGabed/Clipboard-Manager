using ClipboardManager.Helpers;

namespace ClipboardManager.Tests;

/// <summary>
/// Additional TextTransformHelper tests verifying specific expected output values
/// (supplements the round-trip tests already in UnitTest1.cs).
/// </summary>
public class TextTransformHelperAdditionalTests
{
    [Fact]
    public void EncodeBase64_Hello_ReturnsKnownValue()
        => Assert.Equal("SGVsbG8=", TextTransformHelper.EncodeBase64("Hello"));

    [Fact]
    public void DecodeBase64_KnownValue_ReturnsHello()
        => Assert.Equal("Hello", TextTransformHelper.DecodeBase64("SGVsbG8="));

    [Fact]
    public void DecodeBase64_InvalidInput_ReturnsInvalidMarker()
        => Assert.Equal("[Invalid Base64]", TextTransformHelper.DecodeBase64("!!!invalid!!!"));

    [Fact]
    public void UrlEncode_SpaceAndPlus_EncodesCorrectly()
    {
        // URI.EscapeDataString encodes space → %20, + → %2B
        var result = TextTransformHelper.UrlEncode("a b+c");
        Assert.Equal("a%20b%2Bc", result);
    }

    [Fact]
    public void HtmlEncode_AngleBrackets_EncodesCorrectly()
    {
        var result = TextTransformHelper.HtmlEncode("<b>hi</b>");
        Assert.Equal("&lt;b&gt;hi&lt;/b&gt;", result);
    }

    [Fact]
    public void CountCharacters_HelloWorld_ContainsExpectedCounts()
    {
        var result = TextTransformHelper.CountCharacters("hello world");
        // "hello world" = 11 chars, 2 words, 1 line
        Assert.Contains("Characters: 11", result);
        Assert.Contains("Words: 2",       result);
    }

    [Fact]
    public void ReverseText_Abc_ReturnsCba()
        => Assert.Equal("cba", TextTransformHelper.ReverseText("abc"));

    [Fact]
    public void ToTitleCase_HelloWorld_Correct()
        => Assert.Equal("Hello World", TextTransformHelper.ToTitleCase("hello world"));

    [Fact]
    public void ToSentenceCase_LowerInput_CapitalisesFirst()
    {
        var result = TextTransformHelper.ToSentenceCase("hello world");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void TrimWhitespace_PadsAroundText_RemovesBoth()
        => Assert.Equal("hi", TextTransformHelper.TrimWhitespace("  hi  "));

    [Fact]
    public void RemoveExtraSpaces_MultipleSpaces_CollapsesToSingle()
        => Assert.Equal("a b c", TextTransformHelper.RemoveExtraSpaces("a  b   c"));
}
