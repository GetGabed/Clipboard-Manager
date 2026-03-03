using ClipboardManager.Models;

namespace ClipboardManager.Tests;

public class ClipboardItemHelperTests
{
    [Fact]
    public void ImageDimensions_NonZero_ReturnsFormattedString()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Image,
            ImageWidth  = 1920,
            ImageHeight = 1080
        };
        Assert.Equal("1920 × 1080", item.ImageDimensions);
    }

    [Fact]
    public void ImageDimensions_ZeroValues_ReturnsEmpty()
    {
        var item = new ClipboardItem { ContentType = ClipboardContentType.Text };
        Assert.Equal(string.Empty, item.ImageDimensions);
    }

    [Fact]
    public void FilePathsTooltip_MultipleFiles_ReturnsJoinedPaths()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Files,
            FilePaths   = new[] { @"C:\a.txt", @"C:\b.txt" }
        };
        Assert.Contains("\n", item.FilePathsTooltip);
        Assert.Contains(@"C:\a.txt", item.FilePathsTooltip);
    }

    [Fact]
    public void FilePathsTooltip_TextItem_ReturnsNull()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Text,
            TextContent = "hello"
        };
        Assert.Null(item.FilePathsTooltip);
    }
}
