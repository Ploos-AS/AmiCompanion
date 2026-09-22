namespace AmiCompanion.Core.DiskImages;

public static class AdfImage
{
    public static byte[] CreateBlank(byte dosType = 1, bool highDensity = false)
    {
        if (dosType > 7) throw new ArgumentOutOfRangeException(nameof(dosType), "DOS type must be 0 through 7.");
        var size = highDensity ? AdfInspector.HighDensitySize : AdfInspector.StandardSize;
        var image = new byte[size];
        image[0] = (byte)'D'; image[1] = (byte)'O'; image[2] = (byte)'S'; image[3] = dosType;
        return image;
    }
}
