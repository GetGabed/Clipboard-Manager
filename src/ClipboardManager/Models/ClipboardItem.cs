using System;
using System.Windows.Media.Imaging;

namespace ClipboardManager.Models;

/// <summary>Content type of a clipboard entry.</summary>
public enum ClipboardContentType
{
    Text,
    Image,
    Files,
    Other
}

/// <summary>Represents a single captured clipboard entry.</summary>
public class ClipboardItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ClipboardContentType ContentType { get; init; }

    /// <summary>Raw text content (null for non-text items).</summary>
    public string? TextContent { get; init; }

    /// <summary>File paths (null for non-file items).</summary>
    public string[]? FilePaths { get; init; }

    /// <summary>Thumbnail for image items (not persisted to disk).</summary>
    public BitmapSource? ImageThumbnail { get; set; }

    /// <summary>Original pixel width of an image item (0 for non-image items).</summary>
    public int ImageWidth { get; set; }

    /// <summary>Original pixel height of an image item (0 for non-image items).</summary>
    public int ImageHeight { get; set; }

    /// <summary>E.g. "1920 × 1080" — empty for non-image items.</summary>
    public string ImageDimensions =>
        ImageWidth > 0 && ImageHeight > 0 ? $"{ImageWidth} × {ImageHeight}" : string.Empty;

    /// <summary>File-type icon extracted from the first file path (null for non-file items).</summary>
    public BitmapSource? FileIcon { get; set; }

    /// <summary>Multi-line tooltip listing all file paths; null for non-file items (WPF hides null tooltips).</summary>
    public string? FilePathsTooltip =>
        FilePaths is { Length: > 0 } ? string.Join("\n", FilePaths) : null;

    public DateTime CapturedAt { get; init; } = DateTime.Now;

    public bool IsPinned { get; set; }

    /// <summary>Short preview string shown in the history list.</summary>
    public string Preview
    {
        get
        {
            return ContentType switch
            {
                ClipboardContentType.Text => TextContent is { Length: > 120 }
                    ? string.Concat(TextContent.AsSpan(0, 120), "…")
                    : (TextContent ?? string.Empty),
                ClipboardContentType.Image => "[Image]",
                ClipboardContentType.Files => FilePaths is { Length: > 0 }
                    ? $"[{FilePaths.Length} file(s)] {FilePaths[0]}"
                    : "[Files]",
                _ => "[Unknown]"
            };
        }
    }

    /// <summary>Returns true when <paramref name="other"/> has identical content.</summary>
    public bool IsDuplicateOf(ClipboardItem other)
    {
        if (ContentType != other.ContentType)
            return false;

        return ContentType switch
        {
            ClipboardContentType.Text => string.Equals(TextContent, other.TextContent,
                                             StringComparison.Ordinal),
            ClipboardContentType.Files => FilePaths is not null && other.FilePaths is not null
                                          && string.Join("|", FilePaths) == string.Join("|", other.FilePaths),
            _ => false
        };
    }

    public override string ToString() => Preview;
}
