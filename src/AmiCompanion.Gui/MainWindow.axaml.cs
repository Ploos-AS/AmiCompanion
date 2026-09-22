using AmiCompanion.Core.Checksums;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;
using AmiCompanion.Core.ROM;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AmiCompanion.Gui;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Amiga file",
            AllowMultiple = false
        });

        if (files.Count == 1 && files[0].TryGetLocalPath() is { } path)
            FilePathBox.Text = path;
    }

    private async void Checksum_Click(object? sender, RoutedEventArgs e)
    {
        await RunAsync(async path =>
        {
            var info = await ChecksumService.ComputeFileAsync(path);
            return $"File:   {path}\nCRC32:  {info.Crc32:X8}\nSHA256: {info.Sha256}";
        });
    }

    private async void Adf_Click(object? sender, RoutedEventArgs e)
    {
        await RunAsync(async path =>
        {
            var data = await File.ReadAllBytesAsync(path);
            var info = AdfInspector.Inspect(data);
            var text = $"File: {path}\nSize: {info.Size}\nGeometry: {info.Cylinders} cyl / {info.Heads} heads / {info.SectorsPerTrack} sectors\nDOS type: {(info.HasRecognizedDosType ? info.DosType : "unrecognized")}\nStandard geometry: {info.HasStandardGeometry}";
            if (data.Length >= AmigaBootBlockInspector.Size)
            {
                var boot = AmigaBootBlockInspector.Inspect(data);
                text += $"\nBoot checksum: {(boot.IsChecksumValid ? "valid" : "invalid")}\nRoot block: {boot.RootBlock}";
            }
            return text;
        });
    }

    private async void Rom_Click(object? sender, RoutedEventArgs e)
    {
        await RunAsync(async path =>
        {
            var info = KickstartRomInspector.Inspect(await File.ReadAllBytesAsync(path));
            return $"File: {path}\nSize: {info.Size}\nVersion: {info.Version}.{info.Revision}\nExec signature: {info.HasExecSignature}\nChecksum: 0x{info.Checksum:X8} ({(info.IsChecksumValid ? "valid" : "invalid")})";
        });
    }

    private async void Hunk_Click(object? sender, RoutedEventArgs e)
    {
        await RunAsync(async path =>
        {
            var info = HunkInspector.Inspect(await File.ReadAllBytesAsync(path));
            if (!info.HasHeader)
                return $"File: {path}\nHUNK_HEADER: false";
            return $"File: {path}\nHUNK_HEADER: true\nRange: {info.FirstHunk}..{info.LastHunk}\nHunks: {info.HunkSizesLongwords.Count}\nSizes: {string.Join(", ", info.HunkSizesLongwords)} longwords";
        });
    }

    private async Task RunAsync(Func<string, Task<string>> action)
    {
        var path = FilePathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultText.Text = "Select a file first.";
            return;
        }

        try
        {
            ResultText.Text = await action(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            ResultText.Text = $"Error: {ex.Message}";
        }
    }
}
