using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaBootBlockInspectorTests
{
    [Fact]
    public void SyntheticBootBlockChecksumValidates()
    {
        var block = new byte[AmigaBootBlockInspector.Size];
        block[0] = (byte)'D';
        block[1] = (byte)'O';
        block[2] = (byte)'S';
        block[3] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(8, 4), 880);

        var checksum = AmigaBootBlockInspector.CalculateChecksum(block);
        BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(4, 4), checksum);

        var result = AmigaBootBlockInspector.Inspect(block);

        Assert.True(result.HasRecognizedDosType);
        Assert.True(result.IsChecksumValid);
        Assert.Equal("DOS\\1", result.DosType);
        Assert.Equal(880u, result.RootBlock);
        Assert.Equal(checksum, result.StoredChecksum);
    }

    [Fact]
    public void ModifiedBootBlockFailsChecksum()
    {
        var block = new byte[AmigaBootBlockInspector.Size];
        block[0] = (byte)'D';
        block[1] = (byte)'O';
        block[2] = (byte)'S';
        block[3] = 0;
        var checksum = AmigaBootBlockInspector.CalculateChecksum(block);
        BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(4, 4), checksum);
        block[100] ^= 1;

        Assert.False(AmigaBootBlockInspector.Inspect(block).IsChecksumValid);
    }
}
