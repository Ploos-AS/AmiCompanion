using System.Buffers.Binary;
using AmiCompanion.Core.ROM;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class KickstartRomInspectorTests
{
    [Fact]
    public void ReadsSyntheticHeaderWithoutShippingARom()
    {
        var rom = new byte[512 * 1024];
        BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(0, 4), 0x11114ef9);
        BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(12, 2), 40);
        BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(14, 2), 68);

        var info = KickstartRomInspector.Inspect(rom);

        Assert.Equal(524288, info.Size);
        Assert.Equal((ushort)40, info.Version);
        Assert.Equal((ushort)68, info.Revision);
        Assert.True(info.HasExecSignature);
        Assert.False(info.IsChecksumValid);
    }

    [Fact]
    public void CarryAroundChecksumUsesAmigaLongwordRules()
    {
        Span<byte> data = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(data[..4], 0xffffffff);
        BinaryPrimitives.WriteUInt32BigEndian(data[4..], 0xffffffff);

        Assert.Equal(0xffffffffu, KickstartRomInspector.CalculateChecksum(data));
    }
}
