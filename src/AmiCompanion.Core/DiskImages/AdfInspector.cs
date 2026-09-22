using System.Text;

namespace AmiCompanion.Core.DiskImages;

public static class AdfInspector
{
    public const int BytesPerSector = 512;
    public const int Heads = 2;
    public const int DoubleDensitySectorsPerTrack = 11;
    public const int HighDensitySectorsPerTrack = 22;
    public const int SectorsPerTrack = DoubleDensitySectorsPerTrack;
    public const int StandardCylinders = 80;
    public const int StandardSize = BytesPerSector * Heads * DoubleDensitySectorsPerTrack * StandardCylinders;
    public const int HighDensitySize = BytesPerSector * Heads * HighDensitySectorsPerTrack * StandardCylinders;

    public static AdfInfo Inspect(ReadOnlySpan<byte> image)
    {
        var sectors = image.Length == HighDensitySize ? HighDensitySectorsPerTrack : DoubleDensitySectorsPerTrack;
        var cylinderSize = BytesPerSector * Heads * sectors;
        var cylinders = image.Length > 0 && image.Length % cylinderSize == 0 ? image.Length / cylinderSize : 0;
        var standard = cylinders == StandardCylinders && (image.Length == StandardSize || image.Length == HighDensitySize);
        var dosType = image.Length >= 4 ? Encoding.ASCII.GetString(image[..4]) : string.Empty;
        var recognized = image.Length >= 4 && image[0] == (byte)'D' && image[1] == (byte)'O' && image[2] == (byte)'S' && image[3] <= 7;
        return new AdfInfo(image.Length, cylinders, Heads, sectors, BytesPerSector, FormatDosType(image, dosType), recognized, standard);
    }

    private static string FormatDosType(ReadOnlySpan<byte> image, string raw) =>
        image.Length >= 4 && image[0] == (byte)'D' && image[1] == (byte)'O' && image[2] == (byte)'S'
            ? $"DOS\\{image[3]}" : raw;
}
