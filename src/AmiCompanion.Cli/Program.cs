using AmiCompanion.Core;
using AmiCompanion.Core.Checksums;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;
using AmiCompanion.Core.ROM;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return 0;
}

try
{
    return args[0] switch
    {
        "info" => PrintInfo(),
        "version" => PrintVersion(),
        "checksum" when args.Length == 2 => await PrintChecksum(args[1]),
        "adf" when args.Length == 3 && args[1] == "info" => PrintAdf(args[2]),
        "rom" when args.Length == 3 && args[1] == "info" => PrintRom(args[2]),
        "hunk" when args.Length == 3 && args[1] == "info" => PrintHunk(args[2]),
        _ => Unknown(args[0])
    };
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine($"{AppInfo.Name} ({AppInfo.Milestone})");
    Console.WriteLine(AppInfo.Description);
    Console.WriteLine();
    Console.WriteLine("Usage: amic <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  info                 Show application information");
    Console.WriteLine("  version              Show milestone/version information");
    Console.WriteLine("  checksum <file>      Calculate CRC32 and SHA-256");
    Console.WriteLine("  adf info <file>      Inspect an ADF image");
    Console.WriteLine("  rom info <file>      Inspect a Kickstart ROM");
    Console.WriteLine("  hunk info <file>     Inspect an Amiga Hunk executable");
}

static int PrintInfo()
{
    Console.WriteLine(AppInfo.Description);
    return 0;
}

static int PrintVersion()
{
    Console.WriteLine($"{AppInfo.Name} {AppInfo.Milestone}");
    return 0;
}

static async Task<int> PrintChecksum(string path)
{
    var result = await ChecksumService.ComputeFileAsync(path);
    Console.WriteLine($"File    {path}");
    Console.WriteLine($"CRC32   {result.Crc32:X8}");
    Console.WriteLine($"SHA256  {result.Sha256}");
    return 0;
}

static int PrintAdf(string path)
{
    var data = File.ReadAllBytes(path);
    var info = AdfInspector.Inspect(data);
    Console.WriteLine($"File       {path}");
    Console.WriteLine($"Size       {info.Size}");
    Console.WriteLine($"Geometry   {info.Cylinders} cyl / {info.Heads} heads / {info.SectorsPerTrack} sectors");
    Console.WriteLine($"DOS type   {(info.HasRecognizedDosType ? info.DosType : "unrecognized")}");
    Console.WriteLine($"Standard   {info.HasStandardGeometry}");
    if (data.Length >= AmigaBootBlockInspector.Size)
    {
        var boot = AmigaBootBlockInspector.Inspect(data);
        Console.WriteLine($"Boot CRC   {(boot.IsChecksumValid ? "valid" : "invalid")}");
        Console.WriteLine($"Root block {boot.RootBlock}");
    }
    return 0;
}

static int PrintRom(string path)
{
    var data = File.ReadAllBytes(path);
    var info = KickstartRomInspector.Inspect(data);
    Console.WriteLine($"File       {path}");
    Console.WriteLine($"Size       {info.Size}");
    Console.WriteLine($"Version    {info.Version}.{info.Revision}");
    Console.WriteLine($"Signature  {info.HasExecSignature}");
    Console.WriteLine($"Checksum   0x{info.Checksum:X8} ({(info.IsChecksumValid ? "valid" : "invalid")})");
    return 0;
}

static int PrintHunk(string path)
{
    var data = File.ReadAllBytes(path);
    var info = HunkInspector.Inspect(data);
    Console.WriteLine($"File       {path}");
    Console.WriteLine($"Header     {info.HasHeader}");
    if (info.HasHeader)
    {
        Console.WriteLine($"Range      {info.FirstHunk}..{info.LastHunk}");
        Console.WriteLine($"Hunks      {info.HunkSizesLongwords.Count}");
        Console.WriteLine($"Sizes      {string.Join(", ", info.HunkSizesLongwords)} longwords");
    }
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown or incomplete command: {command}");
    Console.Error.WriteLine("Run 'amic --help' for usage.");
    return 2;
}
