using AmiCompanion.Core;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine($"{AppInfo.Name} ({AppInfo.Milestone})");
    Console.WriteLine(AppInfo.Description);
    Console.WriteLine();
    Console.WriteLine("Usage: amic <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  info       Show application information");
    Console.WriteLine("  version    Show milestone/version information");
    return 0;
}

return args[0] switch
{
    "info" => PrintInfo(),
    "version" => PrintVersion(),
    _ => Unknown(args[0])
};

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

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Run 'amic --help' for usage.");
    return 2;
}
