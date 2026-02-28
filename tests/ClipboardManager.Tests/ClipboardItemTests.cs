using ClipboardManager.Models;

namespace ClipboardManager.Tests;

// ── ClipboardItem.Preview ─────────────────────────────────────────────────────

public class ClipboardItem_PreviewTests
{
    [Fact]
    public void Preview_ShortText_ReturnsTextAsIs()
    {
        var item = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "hello" };
        Assert.Equal("hello", item.Preview);
    }

    [Fact]
    public void Preview_TextExactly120Chars_ReturnsWithoutEllipsis()
    {
        var text = new string('x', 120);
        var item = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = text };
        Assert.Equal(text, item.Preview);
        Assert.DoesNotContain("…", item.Preview);
    }

    [Fact]
    public void Preview_TextOver120Chars_TruncatesWithEllipsis()
    {
        var text = new string('a', 150);
        var item = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = text };

        // 120 chars + single "…" character = 121 total
        Assert.Equal(121, item.Preview.Length);
        Assert.EndsWith("…", item.Preview);
        Assert.Equal(new string('a', 120), item.Preview[..120]);
    }

    [Fact]
    public void Preview_ImageType_ReturnsBracketImage()
    {
        var item = new ClipboardItem { ContentType = ClipboardContentType.Image };
        Assert.Equal("[Image]", item.Preview);
    }

    [Fact]
    public void Preview_FilesType_ReturnsBracketNFilesAndFirstPath()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Files,
            FilePaths   = new[] { @"C:\foo\a.txt", @"C:\foo\b.txt" }
        };
        Assert.StartsWith("[2 file(s)]", item.Preview);
        Assert.Contains(@"C:\foo\a.txt", item.Preview);
    }

    [Fact]
    public void Preview_FilesType_EmptyPaths_ReturnsBracketFiles()
    {
        var item = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = Array.Empty<string>() };
        Assert.Equal("[Files]", item.Preview);
    }

    [Fact]
    public void Preview_NullText_ReturnsEmpty()
    {
        var item = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = null };
        Assert.Equal(string.Empty, item.Preview);
    }
}

// ── ClipboardItem.IsDuplicateOf ───────────────────────────────────────────────

public class ClipboardItem_IsDuplicateOfTests
{
    [Fact]
    public void IsDuplicateOf_IdenticalText_ReturnsTrue()
    {
        var a = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "hello" };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "hello" };
        Assert.True(a.IsDuplicateOf(b));
    }

    [Fact]
    public void IsDuplicateOf_DifferentText_ReturnsFalse()
    {
        var a = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "hello" };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "world" };
        Assert.False(a.IsDuplicateOf(b));
    }

    [Fact]
    public void IsDuplicateOf_DifferentContentTypes_ReturnsFalse()
    {
        var a = new ClipboardItem { ContentType = ClipboardContentType.Text,  TextContent = "hello" };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Image };
        Assert.False(a.IsDuplicateOf(b));
    }

    [Fact]
    public void IsDuplicateOf_IdenticalFilePaths_ReturnsTrue()
    {
        var paths = new[] { @"C:\a.txt", @"C:\b.txt" };
        var a = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = paths };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = (string[])paths.Clone() };
        Assert.True(a.IsDuplicateOf(b));
    }

    [Fact]
    public void IsDuplicateOf_DifferentFilePaths_ReturnsFalse()
    {
        var a = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = new[] { @"C:\a.txt" } };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = new[] { @"C:\b.txt" } };
        Assert.False(a.IsDuplicateOf(b));
    }

    [Fact]
    public void IsDuplicateOf_OneHasNullFilePaths_ReturnsFalse()
    {
        var a = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = new[] { @"C:\a.txt" } };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Files, FilePaths = null };
        Assert.False(a.IsDuplicateOf(b));
    }

    [Fact]
    public void IsDuplicateOf_TextCaseSensitive_ReturnsFalse()
    {
        var a = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "Hello" };
        var b = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = "hello" };
        Assert.False(a.IsDuplicateOf(b));
    }
}
