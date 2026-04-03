namespace BusinessLogic.Shared;

/// <summary>
/// Defines the available preview size options for note content in the notes list.
/// </summary>
public enum PreviewSize
{
    /// <summary>
    /// No preview - only title and metadata are shown.
    /// </summary>
    None,

    /// <summary>
    /// Small preview - shows a brief content snippet.
    /// </summary>
    Small,

    /// <summary>
    /// Medium preview - shows a moderate amount of content.
    /// </summary>
    Medium,

    /// <summary>
    /// Large preview - shows an extended content preview.
    /// </summary>
    Large
}
