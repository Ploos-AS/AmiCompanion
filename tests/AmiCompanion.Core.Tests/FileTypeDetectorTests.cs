using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;
using AmiCompanion.Core.Inspect;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class FileTypeDetectorTests
{
    [Fact]
    public void DetectsAdfByGeometryAndDosSignature()
    {
        var data = new byte[AdfInspector.StandardSize];
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 1;
        Assert.Equal(FileKind.Adf, FileTypeDetector.Detect(data));
    }

    [Fact]
    public void DetectsKickstartSignature()
    {
        var data = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(data, 0x11114ef9);
        Assert.Equal(FileKind.KickstartRom, FileTypeDetector.Detect(data));
    }

    [Fact]
    public void DetectsHunkHeader()
    {
        var data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, (uint)HunkType.Header);
        Assert.Equal(FileKind.AmigaHunk, FileTypeDetector.Detect(data));
    }

    [Fact]
    public void DetectsRdbHdfBeyondBlockZero()
    {
        var data = new byte[32 * 512];
        var block = data.AsSpan(3 * 512, 512);
        BinaryPrimitives.WriteUInt32BigEndian(block, 0x5244534B);
        BinaryPrimitives.WriteUInt32BigEndian(block[4..], 64);
        BinaryPrimitives.WriteUInt32BigEndian(block[16..], 512);
        Assert.Equal(FileKind.HdfRdb, FileTypeDetector.Detect(data));
    }

    [Fact]
    public void InvalidRdskCandidateStaysUnknown()
    {
        var data = new byte[32 * 512];
        var block = data.AsSpan(512, 512);
        BinaryPrimitives.WriteUInt32BigEndian(block, 0x5244534B);
        BinaryPrimitives.WriteUInt32BigEndian(block[4..], 63);
        BinaryPrimitives.WriteUInt32BigEndian(block[16..], 512);
        Assert.Equal(FileKind.Unknown, FileTypeDetector.Detect(data));
    }

    [Fact]
    public void UnknownDataStaysUnknown() =>
        Assert.Equal(FileKind.Unknown, FileTypeDetector.Detect(new byte[32]));
}
