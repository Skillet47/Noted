using Markdig;

namespace Noted.Services;

/// <summary>
/// Service for converting Markdown content to HTML for display.
/// Uses Markdig library for parsing and rendering.
/// </summary>
public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseTaskLists()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    /// <summary>
    /// Converts Markdown text to HTML.
    /// </summary>
    /// <param name="markdown">The Markdown content to convert.</param>
    /// <returns>HTML representation of the Markdown content.</returns>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, _pipeline);
    }

    /// <summary>
    /// Extracts plain text from Markdown for preview purposes.
    /// Strips formatting and returns first N characters.
    /// </summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <param name="maxLength">Maximum length of the preview text.</param>
    /// <returns>Plain text preview of the content.</returns>
    public string GetPlainTextPreview(string markdown, int maxLength = 150)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var plainText = Markdown.ToPlainText(markdown, _pipeline);
        plainText = plainText.Replace("\n", " ").Replace("\r", " ").Trim();

        if (plainText.Length <= maxLength)
        {
            return plainText;
        }

        return plainText[..maxLength].TrimEnd() + "...";
    }
}
