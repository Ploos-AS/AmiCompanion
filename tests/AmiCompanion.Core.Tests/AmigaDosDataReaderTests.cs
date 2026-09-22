using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosDataReaderTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ffs)]
    [InlineData(AmigaDosFileSystem.Ofs)]
    public void ReadsFileDataForBothFilesystems(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("TEST", fs);
        const int headerBlock = 100, dataBlock = 101;
        var root = image.AsSpan(880 * 512, 512);
        Write(root, 6, headerBlock); Fix(root, 5);

        var header = image.AsSpan(headerBlock * 512, 512);
        Write(header, 0, 2); Write(header, 3, 1); Write(header, 81, 11); Write(header, 125, dataBlock); Write(header, 127, unchecked((uint)-3));
        header[432] = 4; Encoding.Latin1.GetBytes("test").CopyTo(header[433..]); Fix(header, 5);

        var data = image.AsSpan(dataBlock * 512, 512);
        Write(data, 0, 8); Write(data, 124, 0);
        var payload = Encoding.ASCII.GetBytes("hello world");
        if (fs == AmigaDosFileSystem.Ofs) Write(data, 4, (uint)payload.Length);
        payload.CopyTo(fs == AmigaDosFileSystem.Ffs ? data : data[24..]);

        var output = Path.Combine(Path.GetTempPath(), "amic-data-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, "test")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    static void Write(Span<byte> b,int n,uint v)=>BinaryPrimitives.WriteUInt32BigEndian(b.Slice(n*4,4),v);
    static void Fix(Span<byte> b,int n){Write(b,n,0);uint s=0;for(int i=0;i<128;i++)s=unchecked(s+BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i*4,4)));Write(b,n,unchecked(0u-s));}
}
