using ThymeMe.Application.DataPortability;
using Windows.Storage.Pickers;

namespace ThymeMe.Platform.Windows.DataPortability;

public sealed class WindowsFileDialogService : IUserFileDialogService
{
    private nint windowHandle;

    public ValueTask InitializeAsync(nint windowHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfEqual(windowHandle, nint.Zero);
        this.windowHandle = windowHandle;
        return ValueTask.CompletedTask;
    }

    public async ValueTask<string?> PickSavePathAsync(
        string suggestedFileName,
        string displayName,
        string extension,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);
        string normalizedExtension = NormalizeExtension(extension);
        FileSavePicker picker = new()
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
            DefaultFileExtension = normalizedExtension,
        };
        picker.FileTypeChoices.Add(displayName, [normalizedExtension]);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        global::Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync().AsTask(cancellationToken).ConfigureAwait(false);
        return file?.Path;
    }

    public async ValueTask<string?> PickOpenPathAsync(
        string displayName,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(extensions);
        FileOpenPicker picker = new();
        picker.FileTypeFilter.Clear();
        foreach (string extension in extensions.Take(16))
        {
            picker.FileTypeFilter.Add(NormalizeExtension(extension));
        }

        if (picker.FileTypeFilter.Count == 0)
        {
            throw new ArgumentException("At least one file extension is required.", nameof(extensions));
        }

        _ = displayName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        global::Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync().AsTask(cancellationToken).ConfigureAwait(false);
        return file?.Path;
    }

    private void EnsureInitialized()
    {
        if (windowHandle == nint.Zero)
        {
            throw new InvalidOperationException("The file dialog service must be initialized with a window first.");
        }
    }

    private static string NormalizeExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        string value = extension[0] == '.' ? extension : $".{extension}";
        if (value.Length > 16 || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("The file extension is invalid.", nameof(extension));
        }

        return value;
    }
}
