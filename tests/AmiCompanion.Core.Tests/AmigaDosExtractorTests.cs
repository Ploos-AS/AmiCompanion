using Xunit;
using AmiCompanion.Core.FileSystems;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosExtractorTests
{
    [Fact]
    public void ExtractsEmptySyntheticFileSafely()
    {
        var image = AmigaDosFormatter.FormatAdf("TEST");
        var root = image.AsSpan(880 * 512, 512);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(root.Slice(6 * 4, 4), 100);
        Fix(root, 5);

        var file = image.AsSpan(100 * 512, 512);
        Write(file, 0, 2); Write(file, 81, 0); Write(file, 124, 0); Write(file, 127, unchecked((uint)-3));
        file[432] = 4; System.Text.Encoding.Latin1.GetBytes("test").CopyTo(file[433..]);
        Fix(file, 5);

        var output = Path.Combine(Path.GetTempPath(), "amicompanion-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.True(File.Exists(Path.Combine(output, "test")));
            Assert.Empty(File.ReadAllBytes(Path.Combine(output, "test")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    static void Write(Span<byte> b,int n,uint v)=>System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b.Slice(n*4,4),v);
    static void Fix(Span<byte> b,int n){Write(b,n,0);uint s=0;for(int i=0;i<128;i++)s=unchecked(s+System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i*4,4)));Write(b,n,unchecked(0u-s));}
}
