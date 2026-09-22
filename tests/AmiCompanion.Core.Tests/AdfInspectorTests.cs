using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AdfInspectorTests
{
    [Fact]
    public void Standard880KImageIsRecognized()
    {
        var image = new byte[AdfInspector.StandardSize];
        image[0] = (byte)'D';
        image[1] = (byte)'O';
        image[2] = (byte)'S';
        image[3] = 1;

        var info = AdfInspector.Inspect(image);

        Assert.Equal(901120, info.Size);
        Assert.Equal(80, info.Cylinders);
        Assert.Equal(2, info.Heads);
        Assert.Equal(11, info.SectorsPerTrack);
        Assert.Equal(512, info.BytesPerSector);
        Assert.Equal("DOS\\1", info.DosType);
        Assert.True(info.HasRecognizedDosType);
        Assert.True(info.HasStandardGeometry);
    }

    [Fact]
    public void ArbitraryDataIsNotReportedAsRecognizedDos()
    {
        var info = AdfInspector.Inspect(new byte[1024]);
        Assert.False(info.HasRecognizedDosType);
        Assert.False(info.HasStandardGeometry);
    }
}
