using System.Text;
using System.Text.RegularExpressions;

namespace Noted.Services;

/// <summary>
/// Service for converting Rich Text Format (RTF) content for display.
/// Provides basic RTF to HTML conversion and plain text extraction.
/// </summary>
public partial class RichTextService
{
    /// <summary>
    /// Converts RTF content to HTML for display.
    /// This is a basic converter that handles common RTF formatting.
    /// </summary>
    /// <param name="rtf">The RTF content to convert.</param>
    /// <returns>HTML representation of the RTF content.</returns>
    public string ToHtml(string rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf))
        {
            return string.Empty;
        }

        // If it's not actually RTF, treat it as plain text
        if (!rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.OrdinalIgnoreCase))
        {
            return $"<p>{System.Web.HttpUtility.HtmlEncode(rtf).Replace("\n", "<br/>")}</p>";
        }

        try
        {
            var html = new StringBuilder();
            var plainText = ExtractPlainTextFromRtf(rtf);

            // Convert to HTML with basic formatting preserved
            html.Append("<div class=\"rtf-content\">");

            // Process line by line
            var lines = plainText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    html.Append("<br/>");
                }
                else
                {
                    html.Append($"<p>{System.Web.HttpUtility.HtmlEncode(line)}</p>");
                }
            }

            html.Append("</div>");
            return html.ToString();
        }
        catch
        {
            // If parsing fails, return as plain text
            return $"<p>{System.Web.HttpUtility.HtmlEncode(rtf)}</p>";
        }
    }

    /// <summary>
    /// Extracts plain text from RTF content for preview purposes.
    /// </summary>
    /// <param name="rtf">The RTF content.</param>
    /// <param name="maxLength">Maximum length of the preview text.</param>
    /// <returns>Plain text preview of the content.</returns>
    public string GetPlainTextPreview(string rtf, int maxLength = 150)
    {
        if (string.IsNullOrWhiteSpace(rtf))
        {
            return string.Empty;
        }

        // If it's not actually RTF, just return the text
        if (!rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.OrdinalIgnoreCase))
        {
            var text = rtf.Replace("\n", " ").Replace("\r", " ").Trim();
            return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "...";
        }

        var plainText = ExtractPlainTextFromRtf(rtf);
        plainText = plainText.Replace("\n", " ").Replace("\r", " ").Trim();

        if (plainText.Length <= maxLength)
        {
            return plainText;
        }

        return plainText[..maxLength].TrimEnd() + "...";
    }

    /// <summary>
    /// Extracts plain text from RTF by removing RTF control sequences.
    /// </summary>
    private string ExtractPlainTextFromRtf(string rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf))
        {
            return string.Empty;
        }

        var result = new StringBuilder();
        var inGroup = 0;
        var skipDestination = false;
        var i = 0;

        while (i < rtf.Length)
        {
            var c = rtf[i];

            if (c == '{')
            {
                inGroup++;
                // Check for destination groups to skip
                if (i + 1 < rtf.Length && rtf[i + 1] == '\\')
                {
                    var nextWord = GetControlWord(rtf, i + 1);
                    if (nextWord is "*" or "fonttbl" or "colortbl" or "stylesheet" or "info" or "pict" or "object")
                    {
                        skipDestination = true;
                    }
                }
                i++;
            }
            else if (c == '}')
            {
                inGroup--;
                if (inGroup == 0)
                {
                    skipDestination = false;
                }
                i++;
            }
            else if (c == '\\')
            {
                if (skipDestination)
                {
                    i++;
                    continue;
                }

                // Handle control word
                var controlWord = GetControlWord(rtf, i);
                i += controlWord.Length + 1;

                // Handle special control words
                switch (controlWord)
                {
                    case "par":
                    case "line":
                        result.AppendLine();
                        break;
                    case "tab":
                        result.Append('\t');
                        break;
                    case "'":
                        // Hex character
                        if (i + 2 <= rtf.Length)
                        {
                            var hexValue = rtf.Substring(i, 2);
                            if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var charCode))
                            {
                                result.Append((char)charCode);
                            }
                            i += 2;
                        }
                        break;
                    case "\\":
                        result.Append('\\');
                        break;
                    case "{":
                        result.Append('{');
                        break;
                    case "}":
                        result.Append('}');
                        break;
                }

                // Skip any numeric parameter
                while (i < rtf.Length && (char.IsDigit(rtf[i]) || rtf[i] == '-'))
                {
                    i++;
                }

                // Skip optional space after control word
                if (i < rtf.Length && rtf[i] == ' ')
                {
                    i++;
                }
            }
            else if (!skipDestination && inGroup >= 0)
            {
                result.Append(c);
                i++;
            }
            else
            {
                i++;
            }
        }

        return result.ToString().Trim();
    }

    private static string GetControlWord(string rtf, int startIndex)
    {
        var i = startIndex + 1; // Skip the backslash
        var word = new StringBuilder();

        while (i < rtf.Length && char.IsLetter(rtf[i]))
        {
            word.Append(rtf[i]);
            i++;
        }

        return word.ToString();
    }
}
