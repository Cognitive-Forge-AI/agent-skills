using System.Text;
using System.Text.Json.Nodes;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Generates the plugin table in a marketplace's root <c>README.md</c> from its catalog.</summary>
public static class CatalogReadme
{
    public const string ReadmePath = "README.md";
    public const string StartMarker = "<!-- plugin-table:start - generated from marketplace.json; do not edit -->";
    public const string EndMarker = "<!-- plugin-table:end -->";

    private static readonly char[] LineBreaks = ['\r', '\n', '\u0085', '\u2028', '\u2029'];

    /// <summary>Rewrites the content between the plugin-table markers and leaves the rest of the README untouched.</summary>
    public static void Write(string marketplaceRoot, JsonObject catalog)
    {
        var path = Path.Combine(marketplaceRoot, ReadmePath);
        if (!File.Exists(path)) throw new CatalogException($"{ReadmePath} is missing, so the plugin table cannot be generated.");
        SafePaths.RejectLinks(path);
        // Decoded from bytes because File.ReadAllText would drop a byte-order mark from the preserved text.
        var readme = Encoding.UTF8.GetString(File.ReadAllBytes(path));
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(ReplaceTable(readme, RenderTable(marketplaceRoot, catalog))));
    }

    private static string ReplaceTable(string readme, string table)
    {
        var start = readme.IndexOf(StartMarker, StringComparison.Ordinal);
        var end = readme.IndexOf(EndMarker, StringComparison.Ordinal);
        if (start < 0 || end < start ||
            readme.LastIndexOf(StartMarker, StringComparison.Ordinal) != start ||
            readme.LastIndexOf(EndMarker, StringComparison.Ordinal) != end)
            throw new CatalogException($"{ReadmePath} must contain exactly one plugin-table start marker followed by one end marker.");
        return $"{readme[..(start + StartMarker.Length)]}\n\n{table}\n\n{readme[end..]}";
    }

    private static string RenderTable(string marketplaceRoot, JsonObject catalog)
    {
        var table = new StringBuilder("| Plugin | What it provides | Current version |\n| --- | --- | --- |");
        var plugins = CatalogViews.Plugins(catalog)
            .Select(plugin => (Name: CatalogJson.GetString(plugin, "name") ?? throw new CatalogException("Every catalog plugin needs a name."), Entry: plugin))
            .OrderBy(plugin => plugin.Name, StringComparer.Ordinal);
        foreach (var (name, entry) in plugins)
        {
            var directory = CatalogValidator.PackageDirectory(entry);
            var title = DisplayName(Path.Combine(marketplaceRoot, "plugins", directory, PackageManifests.CodexPath)) ?? name;
            table.Append($"\n| [{Cell(title)}](plugins/{directory}) | {Cell(CatalogJson.GetString(entry, "description"))} | {Cell(CatalogJson.GetString(entry, "version"))} |");
        }
        return table.ToString();
    }

    private static string? DisplayName(string codexManifestPath)
    {
        if (!File.Exists(codexManifestPath)) return null;
        var displayName = CatalogJson.ReadObject(codexManifestPath)["interface"] is JsonObject ui ? CatalogJson.GetString(ui, "displayName") : null;
        return string.IsNullOrWhiteSpace(displayName) ? null : displayName;
    }

    /// <summary>Keeps a catalog value on one table row, as plain text. Encoding <c>&lt;</c> also stops a value from containing a marker.</summary>
    private static string Cell(string? value)
    {
        var line = string.Join(" ", (value ?? "").Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return line.Replace(@"\", @"\\").Replace("|", @"\|").Replace("[", @"\[").Replace("]", @"\]").Replace("<", "&lt;");
    }
}
