using System.Buffers.Binary;

namespace AmiCompanion.Core.DiskImages;

public static class AmigaBootBlockInspector
{
    public const int Size = 1024;

    public static AmigaBootBlock Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
            throw new ArgumentException("An Amiga boot block requires at least 1024 bytes.", nameof(data));

        var block = data[..Size];
        var recognized = block[0] == (byte)'D' && block[1] == (byte)'O' &&
                         block[2] == (byte)'S' && block[3] <= 7;
        var stored = BinaryPrimitives.ReadUInt32BigEndian(block[4..8]);
        var root = BinaryPrimitives.ReadUInt32BigEndian(block[8..12]);
        var calculated = CalculateChecksum(block);

        return new AmigaBootBlock(
            recognized ? $"DOS\\{block[3]}" : string.Empty,
            stored,
            calculated,
            root,
            recognized,
            stored == calculated);
    }

    public static uint CalculateChecksum(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
            throw new ArgumentException("An Amiga boot block requires at least 1024 bytes.", nameof(data));

        uint sum = 0;
        for (var offset = 0; offset < Size; offset += 4)
        {
            uint value = offset == 4 ? 0u : BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
            var previous = sum;
            sum += value;
            if (sum < previous)
                sum++;
        }

        return ~sum;
    }
}
