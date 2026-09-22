using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;

namespace AmiCompanion.Core.Inspect;

public static class FileTypeDetector
{
    private const uint KickstartSignature = 0x11114ef9;
    private const uint TypeMask = 0x3fffffff;

    public static FileKind Detect(ReadOnlySpan<byte> data)
    {
        if (data.Length == AdfInspector.StandardSize &&
            data.Length >= 4 &&
            data[0] == (byte)'D' && data[1] == (byte)'O' && data[2] == (byte)'S' && data[3] <= 7)
            return FileKind.Adf;

        if (data.Length >= 16 && data.Length % 4 == 0 &&
            BinaryPrimitives.ReadUInt32BigEndian(data[..4]) == KickstartSignature)
            return FileKind.KickstartRom;

        if (data.Length >= 4 &&
            (BinaryPrimitives.ReadUInt32BigEndian(data[..4]) & TypeMask) == (uint)HunkType.Header)
            return FileKind.AmigaHunk;

        return FileKind.Unknown;
    }
}
