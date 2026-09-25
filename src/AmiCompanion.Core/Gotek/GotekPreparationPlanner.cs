namespace AmiCompanion.Core.Gotek;

public static class GotekPreparationPlanner
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".adf", ".hfe", ".img", ".ima", ".dsk" };

    public static GotekPreparationPlan Create(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var files = paths
            .Select(Path.GetFullPath)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var entries = new List<GotekImageEntry>(files.Length);
        for (var i = 0; i < files.Length; i++)
            entries.Add(new GotekImageEntry(i, files[i], Path.GetFileName(files[i])));

        return new GotekPreparationPlan(entries);
    }
}
