using AmiCompanion.Core.Checksums;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;
using AmiCompanion.Core.Inspect;
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
        {
            FilePathBox.Text = path;
            await AutoInspectAsync(path);
        }
    }

    private async Task AutoInspectAsync(string path)
    {
        try
        {
            var data = await File.ReadAllBytesAsync(path);
            var kind = FileTypeDetector.Detect(data);
            ResultText.Text = kind switch
            {
                FileKind.Adf => "Detected: ADF image\n" + await FormatAdfAsync(path),
                FileKind.HdfRdb => "Detected: HDF/RDB image\n" + FormatHdf(path, data),
                FileKind.KickstartRom => "Detected: Kickstart ROM\n" + FormatRom(path, data),
                FileKind.AmigaHunk => "Detected: Amiga Hunk\n" + FormatHunk(path, data),
                _ => "Detected: Unknown\nUse Checksums or select an inspector manually."
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        { ResultText.Text = $"Error: {ex.Message}"; }
    }

    private static async Task<string> FormatAdfAsync(string path)
    {
        var data = await File.ReadAllBytesAsync(path); var info = AdfInspector.Inspect(data);
        var text = $"File: {path}\nSize: {info.Size}\nGeometry: {info.Cylinders} cyl / {info.Heads} heads / {info.SectorsPerTrack} sectors\nDOS type: {(info.HasRecognizedDosType ? info.DosType : "unrecognized")}\nStandard geometry: {info.HasStandardGeometry}";
        if (data.Length >= AmigaBootBlockInspector.Size) { var boot=AmigaBootBlockInspector.Inspect(data); text += $"\nBoot checksum: {(boot.IsChecksumValid ? "valid" : "invalid")}\nRoot block: {boot.RootBlock}"; }
        return text;
    }
    private static string FormatHdf(string path, byte[] data)
    {
        var info = RigidDiskImageInspector.Inspect(data);
        var rdb = info.Rdb;
        var lines = new List<string>
        {
            $"File: {path}",
            $"Size: {info.ImageSize}",
            $"RDB offset: {rdb.Offset}",
            $"Block size: {rdb.BlockSize}",
            $"Geometry: {rdb.Cylinders} cyl / {rdb.Heads} heads / {rdb.Sectors} sectors",
            $"RDB checksum: {(rdb.ChecksumValid ? "valid" : "invalid")}"
        };
        foreach (var part in info.Partitions)
            lines.Add($"Partition: {part.Name}  cyl {part.LowCyl}..{part.HighCyl}  {AmigaDosType.Describe(part.DosType)}  pri {part.BootPriority}  checksum {(part.ChecksumValid ? "valid" : "invalid")}");
        foreach (var fs in info.FileSystems)
            lines.Add($"Filesystem: {fs.FileSystemName}  version {fs.Header.Version >> 16}.{fs.Header.Version & 0xffff}  segments {fs.Segments.Count}  bytes {fs.PayloadLength}  checksum {(fs.ChecksumValid ? "valid" : "invalid")}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatRom(string path, byte[] data) { var i=KickstartRomInspector.Inspect(data); return $"File: {path}\nSize: {i.Size}\nVersion: {i.Version}.{i.Revision}\nExec signature: {i.HasExecSignature}\nChecksum: 0x{i.Checksum:X8} ({(i.IsChecksumValid ? "valid" : "invalid")})"; }
    private static string FormatHunk(string path, byte[] data) { var i=HunkInspector.Inspect(data); return i.HasHeader ? $"File: {path}\nHUNK_HEADER: true\nRange: {i.FirstHunk}..{i.LastHunk}\nHunks: {i.HunkSizesLongwords.Count}\nSizes: {string.Join(", ", i.HunkSizesLongwords)} longwords" : $"File: {path}\nHUNK_HEADER: false"; }

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
