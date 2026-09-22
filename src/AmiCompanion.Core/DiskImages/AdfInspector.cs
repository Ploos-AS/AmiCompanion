using System.Text;

namespace AmiCompanion.Core.DiskImages;

public static class AdfInspector
{
    public const int BytesPerSector = 512;
    public const int Heads = 2;
    public const int SectorsPerTrack = 11;
    public const int StandardCylinders = 80;
    public const long StandardSize = (long)BytesPerSector * Heads * SectorsPerTrack * StandardCylinders;

    public static AdfInfo Inspect(ReadOnlySpan<byte> image)
    {
        var standard = image.Length == StandardSize;
        var cylinderSize = BytesPerSector * Heads * SectorsPerTrack;
        var cylinders = image.Length > 0 && image.Length % cylinderSize == 0
            ? image.Length / cylinderSize
            : 0;

        var dosType = image.Length >= 4
            ? Encoding.ASCII.GetString(image[..4])
            : string.Empty;

        var recognized = image.Length >= 4 &&
                         image[0] == (byte)'D' &&
                         image[1] == (byte)'O' &&
                         image[2] == (byte)'S' &&
                         image[3] <= 7;

        return new AdfInfo(
            image.Length,
            cylinders,
            Heads,
            SectorsPerTrack,
            BytesPerSector,
            FormatDosType(image, dosType),
            recognized,
            standard);
    }

    private static string FormatDosType(ReadOnlySpan<byte> image, string raw) =>
        image.Length >= 4 && image[0] == (byte)'D' && image[1] == (byte)'O' && image[2] == (byte)'S'
            ? $"DOS\\{image[3]}"
            : raw;
}
