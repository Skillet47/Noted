namespace Noted.Services;

/// <summary>
/// Cross-platform file and folder picker that wraps native OS dialogs.
/// iOS / macCatalyst → <c>UIDocumentPickerViewController</c> (macCatalyst renders it as a native NSOpenPanel)
/// Other platforms    → returns <c>null</c>
/// </summary>
public class FolderPickerService : IFolderPickerService, IFilesPickerService
{
#if MACCATALYST
    private const string StorageLocationBookmarkKey = "NotesStorageLocationBookmark";
#endif

    /// <inheritdoc/>
    public async Task<string?> PickFolderAsync()
    {
#if IOS || MACCATALYST
        return await PickFolderAppleAsync();
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    /// <inheritdoc/>
    public async Task<string?> PickFileAsync()
    {
#if IOS || MACCATALYST
        return await PickFileAppleAsync();
#else
        await Task.CompletedTask;
        return null;
#endif
    }

#if IOS || MACCATALYST
    private static Task<string?> PickFolderAppleAsync()
    {
        var tcs = new TaskCompletionSource<string?>();

        var folderType = UniformTypeIdentifiers.UTType.CreateFromIdentifier("public.folder")!;
        var picker = new UIKit.UIDocumentPickerViewController(new[] { folderType }, asCopy: false)
        {
            AllowsMultipleSelection = false
        };

        picker.DidPickDocumentAtUrls += (_, args) =>
        {
            var url = args.Urls.FirstOrDefault();
            if (url != null)
            {
                url.StartAccessingSecurityScopedResource();
#if MACCATALYST
                PersistFolderBookmark(url);
#endif
                tcs.TrySetResult(url.Path);
            }
            else
            {
                tcs.TrySetResult(null);
            }
        };

        picker.WasCancelled += (_, _) => tcs.TrySetResult(null);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var presenter = GetTopViewController();
            if (presenter != null)
                presenter.PresentViewController(picker, animated: true, completionHandler: null);
            else
                tcs.TrySetResult(null);
        });

        return tcs.Task;
    }

    private static Task<string?> PickFileAppleAsync()
    {
        var tcs = new TaskCompletionSource<string?>();

        // Allow picking any file type
        var allFilesType = UniformTypeIdentifiers.UTType.CreateFromIdentifier("public.item")!;
        var picker = new UIKit.UIDocumentPickerViewController(new[] { allFilesType }, asCopy: false)
        {
            AllowsMultipleSelection = false
        };

        picker.DidPickDocumentAtUrls += (_, args) =>
        {
            var url = args.Urls.FirstOrDefault();
            if (url != null)
            {
                url.StartAccessingSecurityScopedResource();
                tcs.TrySetResult(url.Path);
            }
            else
            {
                tcs.TrySetResult(null);
            }
        };

        picker.WasCancelled += (_, _) => tcs.TrySetResult(null);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var presenter = GetTopViewController();
            if (presenter != null)
                presenter.PresentViewController(picker, animated: true, completionHandler: null);
            else
                tcs.TrySetResult(null);
        });

        return tcs.Task;
    }

    /// <summary>
    /// Walks the UIWindowScene hierarchy to find the topmost presented view controller.
    /// Iterates all connected scenes so it works correctly on macCatalyst where the
    /// activation state may not be ForegroundActive at the point of the call.
    /// </summary>
    private static UIKit.UIViewController? GetTopViewController()
    {
        UIKit.UIWindow? window = null;

        foreach (var scene in UIKit.UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIKit.UIWindowScene ws) continue;

            // Prefer the key window; keep looking across scenes if not found yet
            var candidate = ws.Windows.FirstOrDefault(w => w.IsKeyWindow)
                         ?? ws.Windows.FirstOrDefault();
            if (candidate?.RootViewController != null)
            {
                window = candidate;
                // Stop as soon as we have the key window so it is preferred
                if (candidate.IsKeyWindow) break;
            }
        }

        // Walk to the topmost presented controller
        var vc = window?.RootViewController;
        while (vc?.PresentedViewController != null)
            vc = vc.PresentedViewController;

        return vc;
    }

#if MACCATALYST
    private static void PersistFolderBookmark(Foundation.NSUrl url)
    {
        try
        {
            var bookmarkData = url.CreateBookmarkData(
#pragma warning disable CA1416
                Foundation.NSUrlBookmarkCreationOptions.WithSecurityScope,
#pragma warning restore CA1416
                null,
                null,
                out var createError);

            if (bookmarkData is null || createError is not null)
                return;

            Preferences.Set(StorageLocationBookmarkKey, Convert.ToBase64String(bookmarkData.ToArray()));
        }
        catch
        {
            // Ignore bookmark persistence failures; folder still works for current session.
        }
    }
#endif
#endif
}
