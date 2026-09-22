using System.Buffers.Binary;

namespace AmiCompanion.Core.ROM;

public static class KickstartRomInspector
{
    private const uint ExecSignature = 0x11114ef9;

    public static KickstartRomInfo Inspect(ReadOnlySpan<byte> rom)
    {
        if (rom.Length < 16 || rom.Length % 4 != 0)
            throw new ArgumentException("Kickstart ROM data must be at least 16 bytes and longword aligned.", nameof(rom));

        var signature = BinaryPrimitives.ReadUInt32BigEndian(rom[..4]) == ExecSignature;
        var version = BinaryPrimitives.ReadUInt16BigEndian(rom.Slice(12, 2));
        var revision = BinaryPrimitives.ReadUInt16BigEndian(rom.Slice(14, 2));
        var checksum = CalculateChecksum(rom);

        return new KickstartRomInfo(
            rom.Length,
            version,
            revision,
            signature,
            checksum,
            checksum == 0xffffffffu);
    }

    public static uint CalculateChecksum(ReadOnlySpan<byte> rom)
    {
        if (rom.Length == 0 || rom.Length % 4 != 0)
            throw new ArgumentException("ROM data must be non-empty and longword aligned.", nameof(rom));

        uint sum = 0;
        for (var offset = 0; offset < rom.Length; offset += 4)
        {
            var value = BinaryPrimitives.ReadUInt32BigEndian(rom.Slice(offset, 4));
            var previous = sum;
            sum += value;
            if (sum < previous)
                sum++;
        }
        return sum;
    }
}
