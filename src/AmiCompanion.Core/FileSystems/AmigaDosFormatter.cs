using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.DiskImages;

namespace AmiCompanion.Core.FileSystems;

public static class AmigaDosFormatter
{
    private const int BlockSize = 512;
    private const uint TypeShort = 2;
    private const uint SecondaryRoot = 1;

    public static byte[] FormatAdf(string volumeName, AmigaDosFileSystem fileSystem = AmigaDosFileSystem.Ffs, bool highDensity = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(volumeName);
        var nameBytes = Encoding.Latin1.GetBytes(volumeName);
        if (nameBytes.Length > 30) throw new ArgumentException("AmigaDOS volume name must be at most 30 bytes.", nameof(volumeName));

        var image = AdfImage.CreateBlank((byte)fileSystem, highDensity);
        var blockCount = image.Length / BlockSize;
        var rootBlock = blockCount / 2;
        var bitmapBlock = rootBlock + 1;

        WriteRootBlock(image.AsSpan(rootBlock * BlockSize, BlockSize), nameBytes, bitmapBlock);
        WriteBitmapBlock(image.AsSpan(bitmapBlock * BlockSize, BlockSize), blockCount, rootBlock, bitmapBlock);
        WriteBootChecksum(image.AsSpan(0, 1024));
        return image;
    }

    private static void WriteRootBlock(Span<byte> block, ReadOnlySpan<byte> name, int bitmapBlock)
    {
        WriteU32(block, 0, TypeShort);
        WriteU32(block, 3, 72);
        WriteU32(block, 78, 0xffffffff);
        WriteU32(block, 79, (uint)bitmapBlock);
        block[432] = (byte)name.Length;
        name.CopyTo(block[433..]);
        WriteU32(block, 127, SecondaryRoot);
        WriteBlockChecksum(block, 5);
    }

    private static void WriteBitmapBlock(Span<byte> block, int blockCount, int rootBlock, int bitmapBlock)
    {
        for (var blockNumber = 2; blockNumber < blockCount; blockNumber++)
        {
            if (blockNumber == rootBlock || blockNumber == bitmapBlock) continue;
            var bit = blockNumber - 2;
            var word = 1 + bit / 32;
            var bitInWord = bit % 32;
            var value = ReadU32(block, word) | (1u << bitInWord);
            WriteU32(block, word, value);
        }
        WriteBlockChecksum(block, 0);
    }

    private static void WriteBootChecksum(Span<byte> boot)
    {
        boot.Slice(4, 4).Clear();
        var checksum = AmigaBootBlockInspector.CalculateChecksum(boot);
        BinaryPrimitives.WriteUInt32BigEndian(boot.Slice(4, 4), checksum);
    }

    private static void WriteBlockChecksum(Span<byte> block, int checksumLongword)
    {
        WriteU32(block, checksumLongword, 0);
        uint sum = 0;
        for (var i = 0; i < BlockSize / 4; i++) sum = unchecked(sum + ReadU32(block, i));
        WriteU32(block, checksumLongword, unchecked(0u - sum));
    }

    private static uint ReadU32(ReadOnlySpan<byte> block, int longword) =>
        BinaryPrimitives.ReadUInt32BigEndian(block.Slice(longword * 4, 4));

    private static void WriteU32(Span<byte> block, int longword, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(block.Slice(longword * 4, 4), value);
}
