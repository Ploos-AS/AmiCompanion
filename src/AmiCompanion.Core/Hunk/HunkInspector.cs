using System.Buffers.Binary;

namespace AmiCompanion.Core.Hunk;

public static class HunkInspector
{
    private const uint TypeMask = 0x3fffffff;

    public static HunkInfo Inspect(ReadOnlySpan<byte> data)
    {
        var offset = 0;
        var first = ReadLong(data, ref offset) & TypeMask;
        if (first != (uint)HunkType.Header)
            return new HunkInfo(false, 0, 0, Array.Empty<uint>());

        // Resident library names precede the hunk table and are encoded as
        // a length in longwords followed by that many longwords of text.
        while (true)
        {
            var nameLength = ReadLong(data, ref offset);
            if (nameLength == 0)
                break;
            SkipLongwords(data, ref offset, nameLength);
        }

        var tableSize = ReadLong(data, ref offset);
        var firstHunk = ReadLong(data, ref offset);
        var lastHunk = ReadLong(data, ref offset);

        if (lastHunk < firstHunk)
            throw new InvalidDataException("Hunk header has an invalid hunk range.");

        var count = checked(lastHunk - firstHunk + 1);
        if (tableSize != count)
            throw new InvalidDataException("Hunk table size does not match the declared range.");

        var sizes = new List<uint>(checked((int)count));
        for (uint i = 0; i < count; i++)
            sizes.Add(ReadLong(data, ref offset) & TypeMask);

        return new HunkInfo(true, firstHunk, lastHunk, sizes);
    }

    private static uint ReadLong(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset > data.Length - 4)
            throw new InvalidDataException("Unexpected end of Amiga Hunk data.");

        var value = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;
        return value;
    }

    private static void SkipLongwords(ReadOnlySpan<byte> data, ref int offset, uint count)
    {
        var bytes = checked((long)count * 4);
        if (bytes > data.Length - offset)
            throw new InvalidDataException("Unexpected end of Amiga Hunk data.");
        offset += checked((int)bytes);
    }
}
