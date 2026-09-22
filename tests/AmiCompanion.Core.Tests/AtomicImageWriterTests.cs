using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AtomicImageWriterTests
{
    [Fact]
    public void ReplacesExistingFile()
    {
        WithTempDirectory(dir =>
        {
            var path = Path.Combine(dir, "disk.adf");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            AtomicImageWriter.Write(path, new byte[] { 4, 5 });
            Assert.Equal(new byte[] { 4, 5 }, File.ReadAllBytes(path));
            Assert.False(File.Exists(path + ".bak"));
            Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
        });
    }

    [Fact]
    public void BackupContainsExactOriginal()
    {
        WithTempDirectory(dir =>
        {
            var path = Path.Combine(dir, "disk.adf");
            var original = new byte[] { 1, 2, 3, 4 };
            File.WriteAllBytes(path, original);
            AtomicImageWriter.Write(path, new byte[] { 9, 8 }, backup: true);
            Assert.Equal(original, File.ReadAllBytes(path + ".bak"));
            Assert.Equal(new byte[] { 9, 8 }, File.ReadAllBytes(path));
        });
    }

    [Fact]
    public void BackupIsReplacedOnSubsequentExplicitBackup()
    {
        WithTempDirectory(dir =>
        {
            var path = Path.Combine(dir, "disk.adf");
            File.WriteAllBytes(path, new byte[] { 1 });
            AtomicImageWriter.Write(path, new byte[] { 2 }, backup: true);
            AtomicImageWriter.Write(path, new byte[] { 3 }, backup: true);
            Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(path + ".bak"));
            Assert.Equal(new byte[] { 3 }, File.ReadAllBytes(path));
        });
    }

    [Fact]
    public void CanCreateNewFileWithBackupRequested()
    {
        WithTempDirectory(dir =>
        {
            var path = Path.Combine(dir, "new.adf");
            AtomicImageWriter.Write(path, new byte[] { 7 }, backup: true);
            Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(path));
            Assert.False(File.Exists(path + ".bak"));
        });
    }

    private static void WithTempDirectory(Action<string> action)
    {
        var dir = Path.Combine(Path.GetTempPath(), "amic-atomic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { action(dir); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
