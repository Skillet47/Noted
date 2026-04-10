namespace Noted.Services;

/// <summary>
/// Opens the system file explorer and reveals a specific file when supported.
/// </summary>
public interface IFileExplorerService
{
    /// <summary>
    /// Opens the system file explorer and attempts to highlight the specified file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <returns>True when the request was issued successfully; otherwise, false.</returns>
    Task<bool> RevealFileAsync(string filePath);
}
