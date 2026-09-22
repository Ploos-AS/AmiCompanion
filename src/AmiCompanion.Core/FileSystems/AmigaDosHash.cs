using System.Text;

namespace AmiCompanion.Core.FileSystems;

public static class AmigaDosHash
{
    public static int GetBucket(string name, int hashTableSize = 72)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (hashTableSize <= 0) throw new ArgumentOutOfRangeException(nameof(hashTableSize));

        var bytes = Encoding.Latin1.GetBytes(name);
        var hash = bytes.Length;
        foreach (var value in bytes)
        {
            var c = value is >= (byte)'a' and <= (byte)'z' ? value - 0x20 : value;
            hash = (hash * 13 + c) & 0x7ff;
        }
        return hash % hashTableSize;
    }
}
