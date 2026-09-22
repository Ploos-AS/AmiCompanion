using AmiCompanion.Core;
using AmiCompanion.Core.Checksums;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;
using AmiCompanion.Core.Inspect;
using AmiCompanion.Core.ROM;

if (args.Length == 0 || args[0] is "-h" or "--help") { PrintHelp(); return 0; }
try
{
    return args[0] switch
    {
        "info" => PrintInfo(), "version" => PrintVersion(),
        "inspect" when args.Length == 2 => PrintAuto(args[1]),
        "checksum" when args.Length == 2 => await PrintChecksum(args[1]),
        "adf" when args.Length == 3 && args[1] == "info" => PrintAdf(args[2]),
        "rom" when args.Length == 3 && args[1] == "info" => PrintRom(args[2]),
        "hunk" when args.Length == 3 && args[1] == "info" => PrintHunk(args[2]),
        _ => Unknown(args[0])
    };
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
{ Console.Error.WriteLine($"error: {ex.Message}"); return 1; }

static void PrintHelp()
{
    Console.WriteLine($"{AppInfo.Name} ({AppInfo.Milestone})\n{AppInfo.Description}\n\nUsage: amic <command> [options]\n");
    Console.WriteLine("Commands:\n  info                 Show application information\n  version              Show milestone/version information\n  inspect <file>       Auto-detect and inspect a file\n  checksum <file>      Calculate CRC32 and SHA-256\n  adf info <file>      Inspect an ADF image\n  rom info <file>      Inspect a Kickstart ROM\n  hunk info <file>     Inspect an Amiga Hunk executable");
}
static int PrintInfo(){ Console.WriteLine(AppInfo.Description); return 0; }
static int PrintVersion(){ Console.WriteLine($"{AppInfo.Name} {AppInfo.Milestone}"); return 0; }
static int PrintAuto(string path)
{
    var data=File.ReadAllBytes(path);
    var kind=FileTypeDetector.Detect(data);
    Console.WriteLine($"Detected   {kind}");
    return kind switch { FileKind.Adf=>PrintAdf(path), FileKind.KickstartRom=>PrintRom(path), FileKind.AmigaHunk=>PrintHunk(path), _=>0 };
}
static async Task<int> PrintChecksum(string path)
{
    var r=await ChecksumService.ComputeFileAsync(path);
    Console.WriteLine($"File    {path}\nCRC32   {r.Crc32:X8}\nSHA256  {r.Sha256}"); return 0;
}
static int PrintAdf(string path)
{
    var data=File.ReadAllBytes(path); var i=AdfInspector.Inspect(data);
    Console.WriteLine($"File       {path}\nSize       {i.Size}\nGeometry   {i.Cylinders} cyl / {i.Heads} heads / {i.SectorsPerTrack} sectors\nDOS type   {(i.HasRecognizedDosType?i.DosType:"unrecognized")}\nStandard   {i.HasStandardGeometry}");
    if(data.Length>=AmigaBootBlockInspector.Size){var b=AmigaBootBlockInspector.Inspect(data); Console.WriteLine($"Boot CRC   {(b.IsChecksumValid?"valid":"invalid")}\nRoot block {b.RootBlock}");} return 0;
}
static int PrintRom(string path)
{
    var i=KickstartRomInspector.Inspect(File.ReadAllBytes(path));
    Console.WriteLine($"File       {path}\nSize       {i.Size}\nVersion    {i.Version}.{i.Revision}\nSignature  {i.HasExecSignature}\nChecksum   0x{i.Checksum:X8} ({(i.IsChecksumValid?"valid":"invalid")})"); return 0;
}
static int PrintHunk(string path)
{
    var i=HunkInspector.Inspect(File.ReadAllBytes(path)); Console.WriteLine($"File       {path}\nHeader     {i.HasHeader}");
    if(i.HasHeader) Console.WriteLine($"Range      {i.FirstHunk}..{i.LastHunk}\nHunks      {i.HunkSizesLongwords.Count}\nSizes      {string.Join(", ",i.HunkSizesLongwords)} longwords"); return 0;
}
static int Unknown(string command){Console.Error.WriteLine($"Unknown or incomplete command: {command}\nRun 'amic --help' for usage."); return 2;}
