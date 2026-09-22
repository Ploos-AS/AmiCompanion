using System.Buffers.Binary;
using AmiCompanion.Core.Hunk;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class HunkInspectorTests
{
    [Fact]
    public void ReadsSyntheticExecutableHeader()
    {
        var data = new byte[28];
        var words = new uint[] { 1011, 0, 2, 0, 1, 16, 8 };
        for (var i = 0; i < words.Length; i++)
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(i * 4, 4), words[i]);

        var info = HunkInspector.Inspect(data);

        Assert.True(info.HasHeader);
        Assert.Equal(0u, info.FirstHunk);
        Assert.Equal(1u, info.LastHunk);
        Assert.Equal(new uint[] { 16, 8 }, info.HunkSizesLongwords);
    }

    [Fact]
    public void RejectsTruncatedHeader()
    {
        var data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, 1011);
        Assert.Throws<InvalidDataException>(() => HunkInspector.Inspect(data));
    }

    [Fact]
    public void NonHunkFileIsReportedWithoutParsing()
    {
        Span<byte> data = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, 0x12345678);
        Assert.False(HunkInspector.Inspect(data).HasHeader);
    }
}
