namespace AmiCompanion.Core.DiskImages;

public static class AmigaDosType
{
    public static string Describe(uint value) => value switch
    {
        0x444f5300 => "OFS",
        0x444f5301 => "FFS",
        0x444f5302 => "OFS international",
        0x444f5303 => "FFS international",
        0x444f5304 => "OFS directory cache",
        0x444f5305 => "FFS directory cache",
        0x444f5306 => "OFS long filenames",
        0x444f5307 => "FFS long filenames",
        0x50465300 => "PFS",
        0x50465301 => "PFS",
        0x50465302 => "PFS",
        0x53465300 => "SFS",
        0x53465302 => "SFS",
        _ => "unknown"
    };
}
