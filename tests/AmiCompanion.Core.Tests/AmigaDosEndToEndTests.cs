using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosEndToEndTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void CreateListExtractRoundTripsFile(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("ROUNDTRIP", fs);
        const int headerBlock = 100;
        const int dataBlock = 101;
        const string name = "hello.txt";
        var payload = Encoding.UTF8.GetBytes("AmiCompanion M2 round-trip test.");

        var root = image.AsSpan(880 * 512, 512);
        Write(root, 6, headerBlock); Fix(root, 5);

        var header = image.AsSpan(headerBlock * 512, 512);
        Write(header, 0, 2); Write(header, 2, 1); Write(header, 3, 0); Write(header, 4, dataBlock); Write(header, 77, dataBlock); Write(header, 81, (uint)payload.Length);
        Write(header, 127, unchecked((uint)-3)); header[432] = (byte)name.Length;
        Encoding.Latin1.GetBytes(name).CopyTo(header[433..]); Fix(header, 5);

        var data = image.AsSpan(dataBlock * 512, 512);
        if (fs == AmigaDosFileSystem.Ofs)
        {
            Write(data, 0, 8); Write(data, 1, headerBlock); Write(data, 2, 1); Write(data, 3, (uint)payload.Length); Write(data, 4, 0);
            payload.CopyTo(data[24..]); Fix(data, 5);
        }
        else payload.CopyTo(data);

        var listed = AmigaDosReader.ListAll(image);
        var listedEntry = Assert.Single(listed);
        Assert.Equal(name, listedEntry.Path);
        Assert.Equal((uint)payload.Length, listedEntry.Entry.ByteSize);

        var output = Path.Combine(Path.GetTempPath(), "amic-e2e-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, name)));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    static void Write(Span<byte> b, int n, uint v) =>
        BinaryPrimitives.WriteUInt32BigEndian(b.Slice(n * 4, 4), v);

    static void Fix(Span<byte> b, int n)
    {
        Write(b, n, 0); uint sum = 0;
        for (var i = 0; i < 128; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i * 4, 4)));
        Write(b, n, unchecked(0u - sum));
    }
}
