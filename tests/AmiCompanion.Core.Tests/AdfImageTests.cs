using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AdfImageTests
{
    [Theory]
    [InlineData(false, 901120, 11)]
    [InlineData(true, 1802240, 22)]
    public void CreatesRecognizableBlankImage(bool highDensity, int expectedSize, int sectors)
    {
        var image = AdfImage.CreateBlank(1, highDensity);
        var info = AdfInspector.Inspect(image);
        Assert.Equal(expectedSize, image.Length);
        Assert.True(info.HasRecognizedDosType);
        Assert.True(info.HasStandardGeometry);
        Assert.Equal(80, info.Cylinders);
        Assert.Equal(sectors, info.SectorsPerTrack);
        Assert.Equal("DOS\\1", info.DosType);
    }

    [Fact]
    public void RejectsInvalidDosType() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AdfImage.CreateBlank(8));
}
