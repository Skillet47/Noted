using System.Diagnostics;

namespace Noted.Services;

public class FileExplorerService : IFileExplorerService
{
    public Task<bool> RevealFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult(false);

        try
        {
#if MACCATALYST
            return Task.FromResult(TryStartProcess("open", "-R", filePath));
#elif WINDOWS
            return Task.FromResult(TryStartProcess("explorer.exe", $"/select,{filePath}"));
#elif LINUX
            var folderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(folderPath))
                return Task.FromResult(false);

            return Task.FromResult(TryStartProcess("xdg-open", folderPath));
#else
            return Task.FromResult(false);
#endif
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static bool TryStartProcess(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = Process.Start(startInfo);
        return process is not null;
    }
}
