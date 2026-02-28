using System.Globalization;
using System.Text;
using System.Web;

namespace ClipboardManager.Helpers;

/// <summary>Text transformation utilities available in the history window.</summary>
public static class TextTransformHelper
{
    public static string ToUpperCase(string text)    => text.ToUpper(CultureInfo.CurrentCulture);
    public static string ToLowerCase(string text)    => text.ToLower(CultureInfo.CurrentCulture);
    public static string ToTitleCase(string text)    => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    public static string ToSentenceCase(string text) => string.IsNullOrWhiteSpace(text)
        ? text
        : char.ToUpper(text[0]) + text[1..];

    public static string TrimWhitespace(string text) => text.Trim();
    public static string RemoveExtraSpaces(string text)
        => System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

    public static string EncodeBase64(string text)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    public static string DecodeBase64(string text)
    {
        try   { return Encoding.UTF8.GetString(Convert.FromBase64String(text)); }
        catch { return "[Invalid Base64]"; }
    }

    public static string UrlEncode(string text)  => Uri.EscapeDataString(text);
    public static string UrlDecode(string text)  => Uri.UnescapeDataString(text);

    public static string HtmlEncode(string text) => HttpUtility.HtmlEncode(text);
    public static string HtmlDecode(string text) => HttpUtility.HtmlDecode(text);

    public static string ReverseText(string text)
    {
        var chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public static string CountCharacters(string text)
        => $"Characters: {text.Length}  Words: {CountWords(text)}  Lines: {text.Split('\n').Length}";

    private static int CountWords(string text)
        => string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(new[] { ' ', '\t', '\n', '\r' },
                          StringSplitOptions.RemoveEmptyEntries).Length;
}
