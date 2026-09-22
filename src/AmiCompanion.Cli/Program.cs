using AmiCompanion.Core;
using AmiCompanion.Core.Checksums;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.Hunk;
using AmiCompanion.Core.Inspect;
using AmiCompanion.Core.FileSystems;
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
        "adf" when args.Length == 3 && args[1] == "list" => ListAdf(args[2]),
        "adf" when args.Length == 4 && args[1] == "extract" => ExtractAdf(args[2], args[3]),
        "adf" when args.Length is 4 or 5 && args[1] == "create" => CreateAdf(args),
        "adf" when args.Length is 5 or 6 && args[1] == "put" => PutAdf(args[2], args[3], args[4], args.Length == 6 && args[5] == "--backup"),
        "adf" when args.Length is 4 or 5 && args[1] == "mkdir" => MkdirAdf(args[2], args[3], args.Length == 5 && args[4] == "--backup"),
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
    Console.WriteLine("Commands:\n  info                 Show application information\n  version              Show milestone/version information\n  inspect <file>       Auto-detect and inspect a file\n  checksum <file>      Calculate CRC32 and SHA-256\n  adf info <file>      Inspect an ADF image\n  adf list <file>      List the root directory\n  adf extract <file> <output>  Extract an AmigaDOS volume\n  adf create <file> <label> [ofs|ffs]  Create a formatted DD ADF\n  adf put <adf> <source> <path> [--backup]  Add a file to an ADF\n  adf mkdir <adf> <path> [--backup]  Create a directory in an ADF\n  rom info <file>      Inspect a Kickstart ROM\n  hunk info <file>     Inspect an Amiga Hunk executable");
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
static int PutAdf(string adfPath, string sourcePath, string name, bool backup)
{
    var image = File.ReadAllBytes(adfPath);
    var fs = AmigaDosReader.Inspect(image).FileSystem;
    var content = File.ReadAllBytes(sourcePath);
    var normalized = name.Replace('\\', '/').Trim('/');
    var slash = normalized.LastIndexOf('/');
    var directory = slash < 0 ? string.Empty : normalized[..slash];
    var fileName = slash < 0 ? normalized : normalized[(slash + 1)..];
    AmigaDosFileWriter.AddFile(image, fs, directory, fileName, content);
    AtomicImageWriter.Write(adfPath, image, backup);
    Console.WriteLine($"Added      {name}\nADF        {adfPath}\nBytes      {content.Length}");
    return 0;
}
static int MkdirAdf(string adfPath, string path, bool backup)
{
    var image = File.ReadAllBytes(adfPath);
    var fs = AmigaDosReader.Inspect(image).FileSystem;
    var parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) throw new ArgumentException("Directory path cannot be empty.", nameof(path));
    var parent = string.Empty;
    foreach (var part in parts)
    {
        var full = parent.Length == 0 ? part : parent + "/" + part;
        var exists = AmigaDosReader.ListAll(image).Any(x => x.Entry.IsDirectory && string.Equals(x.Path, full, StringComparison.OrdinalIgnoreCase));
        if (!exists) AmigaDosFileWriter.CreateDirectory(image, fs, parent, part);
        parent = full;
    }
    AtomicImageWriter.Write(adfPath, image, backup);
    Console.WriteLine($"Created    {path}\nADF        {adfPath}");
    return 0;
}
static int ExtractAdf(string path, string output)
{
    var data = File.ReadAllBytes(path);
    AmigaDosExtractor.Extract(data, output);
    Console.WriteLine($"Extracted  {path}\nOutput     {Path.GetFullPath(output)}");
    return 0;
}
static int ListAdf(string path)
{
    var data = File.ReadAllBytes(path);
    var volume = AmigaDosReader.Inspect(data);
    Console.WriteLine($"Volume     {volume.Name}\nFilesystem {volume.FileSystem}");
    foreach (var item in AmigaDosReader.ListAll(data))
    {
        var entry = item.Entry;
        Console.WriteLine($"{(entry.IsDirectory ? "DIR " : entry.IsFile ? "FILE" : "????")} {entry.ByteSize,10} {item.Path}");
    }
    return 0;
}
static int CreateAdf(string[] commandArgs)
{
    var path = commandArgs[2];
    var label = commandArgs[3];
    var fs = commandArgs.Length == 5 ? commandArgs[4].ToLowerInvariant() switch
    {
        "ofs" => AmigaDosFileSystem.Ofs,
        "ffs" => AmigaDosFileSystem.Ffs,
        _ => throw new ArgumentException("Filesystem must be 'ofs' or 'ffs'.")
    } : AmigaDosFileSystem.Ffs;
    if (File.Exists(path)) throw new IOException($"Refusing to overwrite existing file: {path}");
    File.WriteAllBytes(path, AmigaDosFormatter.FormatAdf(label, fs));
    Console.WriteLine($"Created    {path}\nFilesystem {fs}\nLabel      {label}\nSize       {AdfInspector.StandardSize}");
    return 0;
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
