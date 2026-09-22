using System.Buffers.Binary;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosWriterTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void AddsFileAndRoundTrips(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("WRITE", fs);
        var payload = System.Text.Encoding.UTF8.GetBytes("writer test");
        AmigaDosFileWriter.AddFile(image, fs, "hello.txt", payload);

        var entry = Assert.Single(AmigaDosReader.ListAll(image));
        Assert.Equal("hello.txt", entry.Path);

        var output = Path.Combine(Path.GetTempPath(), "amic-writer-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, "hello.txt")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    [Fact]
    public void RejectsDuplicateFile()
    {
        var image = AmigaDosFormatter.FormatAdf("WRITE");
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "hello", new byte[] { 1 });
        Assert.Throws<IOException>(() => AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "hello", new byte[] { 2 }));
    }

    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void AddsFileToExistingDirectory(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("WRITE", fs);
        const int dirBlock = 100;
        var root = image.AsSpan(880 * 512, 512);
        Write(root, 6, dirBlock); Fix(root, 5);
        var dir = image.AsSpan(dirBlock * 512, 512);
        Write(dir, 0, 2); Write(dir, 125, 880); Write(dir, 127, 2);
        dir[432] = 1; dir[433] = (byte)'C'; Fix(dir, 5);

        var payload = System.Text.Encoding.ASCII.GetBytes("nested writer test");
        AmigaDosFileWriter.AddFile(image, fs, "C", "tool", payload);
        Assert.Contains(AmigaDosReader.ListAll(image), x => x.Path == "C/tool" && x.Entry.ByteSize == payload.Length);

        var output = Path.Combine(Path.GetTempPath(), "amic-writer-dir-" + Guid.NewGuid().ToString("N"));
        try { AmigaDosExtractor.Extract(image, output); Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, "C", "tool"))); }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    [Fact]
    public void RejectsMissingDirectory()
    {
        var image = AmigaDosFormatter.FormatAdf("WRITE");
        Assert.Throws<DirectoryNotFoundException>(() => AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "Missing", "file", new byte[] { 1 }));
    }

    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void CreatesNestedDirectoriesAndRoundTripsFile(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("TREE", fs);
        AmigaDosFileWriter.CreateDirectory(image, fs, "", "C");
        AmigaDosFileWriter.CreateDirectory(image, fs, "C", "Tools");
        AmigaDosFileWriter.CreateDirectory(image, fs, "C/Tools", "AmiCompanion");
        var payload = System.Text.Encoding.ASCII.GetBytes("nested tree");
        AmigaDosFileWriter.AddFile(image, fs, "C/Tools/AmiCompanion", "readme.txt", payload);

        var entries = AmigaDosReader.ListAll(image);
        Assert.Contains(entries, x => x.Path == "C" && x.Entry.IsDirectory);
        Assert.Contains(entries, x => x.Path == "C/Tools" && x.Entry.IsDirectory);
        Assert.Contains(entries, x => x.Path == "C/Tools/AmiCompanion" && x.Entry.IsDirectory);
        Assert.Contains(entries, x => x.Path == "C/Tools/AmiCompanion/readme.txt" && x.Entry.IsFile);

        var output = Path.Combine(Path.GetTempPath(), "amic-writer-tree-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, "C", "Tools", "AmiCompanion", "readme.txt")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    [Fact]
    public void RejectsDuplicateDirectory()
    {
        var image = AmigaDosFormatter.FormatAdf("TREE");
        AmigaDosFileWriter.CreateDirectory(image, AmigaDosFileSystem.Ffs, "", "C");
        Assert.Throws<IOException>(() => AmigaDosFileWriter.CreateDirectory(image, AmigaDosFileSystem.Ffs, "", "C"));
    }

    [Fact]
    public void RejectsDirectoryWithMissingParent()
    {
        var image = AmigaDosFormatter.FormatAdf("TREE");
        Assert.Throws<DirectoryNotFoundException>(() => AmigaDosFileWriter.CreateDirectory(image, AmigaDosFileSystem.Ffs, "C", "Tools"));
    }

    private static void Write(Span<byte> b, int n, uint v) => BinaryPrimitives.WriteUInt32BigEndian(b.Slice(n * 4, 4), v);
    private static void Fix(Span<byte> b, int n) { Write(b, n, 0); uint s = 0; for (var i = 0; i < 128; i++) s = unchecked(s + BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i * 4, 4))); Write(b, n, unchecked(0u - s)); }
}
