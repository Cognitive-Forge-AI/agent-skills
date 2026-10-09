using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Validates a marketplace checkout: catalog, packages, manifests, components, views, and policy.</summary>
public static partial class CatalogValidator
{
    private const long MaxScannedFileBytes = 2 * 1024 * 1024;

    public static IReadOnlyList<string> Validate(string marketplaceRoot, ValidationPolicy? policy = null)
    {
        policy ??= new ValidationPolicy();
        var root = Path.GetFullPath(marketplaceRoot);
        var errors = new List<string>();

        JsonObject catalog;
        try
        {
            catalog = CatalogJson.ReadObject(Path.Combine(root, CatalogViews.RootPath));
        }
        catch (Exception error) when (error is CatalogException or IOException or UnauthorizedAccessException)
        {
            errors.Add($"Cannot read {CatalogViews.RootPath}: {error.Message}");
            return errors;
        }

        if (!Names.IsPluginName(CatalogJson.GetString(catalog, "name")))
            errors.Add("Marketplace name must be kebab-case.");
        if (!Names.IsStableVersion(CatalogJson.GetString(catalog, "version")))
            errors.Add("Marketplace version must be a stable major.minor.patch version.");
        if (catalog["owner"] is not JsonObject owner || string.IsNullOrWhiteSpace(CatalogJson.GetString(owner, "name")))
            errors.Add("Marketplace owner.name is required.");
        if (catalog["plugins"] is not JsonArray plugins)
        {
            errors.Add("Marketplace plugins must be an array.");
            return errors;
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in plugins)
        {
            if (node is not JsonObject entry)
            {
                errors.Add("Each marketplace plugin entry must be an object.");
                continue;
            }
            var name = CatalogJson.GetString(entry, "name");
            if (!Names.IsPluginName(name))
            {
                errors.Add($"Invalid plugin name: {name ?? "(missing)"}.");
                continue;
            }
            if (!names.Add(name!)) errors.Add($"Duplicate plugin name: {name}.");
            try
            {
                ValidatePackage(root, entry, name!, policy, directories, errors);
            }
            catch (Exception error) when (error is CatalogException or IOException or UnauthorizedAccessException)
            {
                errors.Add($"Plugin {name}: {error.Message}");
            }
        }

        foreach (var (relative, expected) in CatalogViews.Create(catalog))
        {
            var path = Path.Combine(root, relative);
            try
            {
                if (!File.Exists(path)) errors.Add($"Generated view {relative} is missing; regenerate views.");
                else if (!JsonNode.DeepEquals(expected, CatalogJson.ReadObject(path)))
                    errors.Add($"Generated view {relative} does not match {CatalogViews.RootPath}; regenerate views.");
            }
            catch (Exception error) when (error is CatalogException or IOException or UnauthorizedAccessException)
            {
                errors.Add($"Generated view {relative}: {error.Message}");
            }
        }

        var pluginsRoot = Path.Combine(root, "plugins");
        if (policy.RequireReferencedPackages && Directory.Exists(pluginsRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(pluginsRoot).Order(StringComparer.Ordinal))
            {
                var directoryName = Path.GetFileName(directory);
                if (!directories.Contains(directoryName))
                    errors.Add($"Package directory plugins/{directoryName} is not referenced by {CatalogViews.RootPath}.");
            }
        }

        return errors;
    }

    /// <summary>Returns the package directory name for a catalog source of the form <c>./plugins/{directory}</c>.</summary>
    public static string PackageDirectory(JsonObject entry)
    {
        var source = CatalogJson.GetString(entry, "source");
        const string prefix = "./plugins/";
        if (source is null || !source.StartsWith(prefix, StringComparison.Ordinal) || !Names.IsPackageDirectory(source[prefix.Length..]))
            throw new CatalogException($"Plugin source must be ./plugins/{{directory}}: {source ?? "(missing)"}");
        return source[prefix.Length..];
    }

    private static void ValidatePackage(string root, JsonObject entry, string name, ValidationPolicy policy, HashSet<string> directories, List<string> errors)
    {
        var version = CatalogJson.GetString(entry, "version");
        if (!Names.IsVersion(version)) errors.Add($"Plugin {name} has an invalid version: {version ?? "(missing)"}.");
        var directory = PackageDirectory(entry);
        if (!directories.Add(directory)) errors.Add($"Plugin {name} shares package directory plugins/{directory} with another entry.");
        CheckMetadata(entry, $"Catalog entry {name}", policy, errors);

        var package = Path.Combine(root, "plugins", directory);
        if (!Directory.Exists(package))
        {
            errors.Add($"Plugin {name} package is missing: plugins/{directory}.");
            return;
        }
        var files = SafePaths.EnumerateFiles(package).ToList();

        var strict = entry["strict"] is not JsonValue strictValue || !strictValue.TryGetValue<bool>(out var isStrict) || isStrict;
        var declaresComponent = false;
        foreach (var manifestPath in PackageManifests.All)
        {
            var required = manifestPath switch
            {
                PackageManifests.ClaudePath => strict,
                PackageManifests.CursorPath => policy.RequireCursorManifest,
                _ => true
            };
            var path = Path.Combine(package, manifestPath);
            if (!File.Exists(path))
            {
                if (required) errors.Add($"Plugin {name} is missing {manifestPath}.");
                continue;
            }
            var manifest = CatalogJson.ReadObject(path);
            if (CatalogJson.GetString(manifest, "name") != name || CatalogJson.GetString(manifest, "version") != version)
                errors.Add($"Plugin {name} {manifestPath} name/version does not match the catalog.");
            CheckMetadata(manifest, $"Plugin {name} {manifestPath}", policy, errors);
            foreach (var key in PackageManifests.ComponentKeys)
            {
                foreach (var reference in CatalogJson.StringValues(manifest[key]))
                {
                    declaresComponent = true;
                    if (reference.Contains('*') || reference.Contains("://", StringComparison.Ordinal)) continue;
                    var resolved = SafePaths.Resolve(package, reference, $"Plugin {name} {manifestPath} {key}");
                    if (!File.Exists(resolved) && !Directory.Exists(resolved))
                        errors.Add($"Plugin {name} {manifestPath} declares missing {key} component: {reference}.");
                }
            }
        }

        if (!declaresComponent)
            errors.Add($"Plugin {name} declares no components; add skills, agents, commands, hooks, MCP servers, or apps.");

        if (policy.RequiredLicense is not null && !policy.LicenseFiles.Any(file => RootFileExists(package, file, "licenseFiles")))
            errors.Add($"Plugin {name} must include a license file ({string.Join(", ", policy.LicenseFiles)}).");
        foreach (var required in policy.RequiredPackageFiles)
            if (!RootFileExists(package, required, "requiredPackageFiles"))
                errors.Add($"Plugin {name} must include {required} at its root.");

        if (policy.RequireSkillLicense)
        {
            foreach (var skill in files.Where(file => Path.GetFileName(file) == "SKILL.md"))
            {
                var license = FrontmatterValue(File.ReadAllText(skill), "license");
                var location = SafePaths.ToPortableRelative(package, skill);
                if (string.IsNullOrWhiteSpace(license))
                    errors.Add($"Plugin {name} {location} must declare license in its frontmatter.");
                else if (policy.RequiredLicense is not null && license != policy.RequiredLicense)
                    errors.Add($"Plugin {name} {location} must declare license {policy.RequiredLicense}, not {license}.");
            }
        }

        if (policy.ForbiddenUrlPrefixes.Count > 0)
        {
            var excluded = new HashSet<string>(policy.UrlScanExcludedPaths, StringComparer.Ordinal);
            foreach (var file in files.Where(file => !excluded.Contains(SafePaths.ToPortableRelative(package, file))))
                ScanUrls(package, file, name, policy, errors);
        }
    }

    /// <summary>Checks for a policy-named file at the package root; policy entries cannot reach outside the package.</summary>
    private static bool RootFileExists(string package, string fileName, string setting)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(['/', '\\']) >= 0 || fileName is "." or ".." || Path.IsPathRooted(fileName))
            throw new CatalogException($"Policy {setting} entries must be file names at the package root: {fileName}");
        return File.Exists(SafePaths.Resolve(package, fileName, $"Policy {setting} entry"));
    }

    /// <summary>Returns a top-level scalar from a Markdown file's YAML frontmatter, or null.</summary>
    public static string? FrontmatterValue(string markdown, string key)
    {
        var text = markdown.ReplaceLineEndings("\n");
        if (!text.StartsWith("---\n", StringComparison.Ordinal)) return null;
        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return null;
        foreach (var line in text[4..end].Split('\n'))
        {
            if (!line.StartsWith(key + ":", StringComparison.Ordinal)) continue;
            var value = line[(key.Length + 1)..].Trim();
            if (value.Length > 0 && value[0] is '"' or '\'')
            {
                // A quoted scalar ends at its closing quote, so a # inside the quotes is part of the value.
                var close = value.IndexOf(value[0], 1);
                return close > 0 ? value[1..close] : value;
            }
            // An unquoted scalar ends where a YAML comment starts: a # preceded by whitespace.
            var comment = FindComment(value);
            return comment >= 0 ? value[..comment].TrimEnd() : value;
        }
        return null;

        static int FindComment(string value)
        {
            for (var index = 1; index < value.Length; index++)
                if (value[index] == '#' && value[index - 1] is ' ' or '\t') return index;
            return -1;
        }
    }

    private static void CheckMetadata(JsonObject json, string description, ValidationPolicy policy, List<string> errors)
    {
        if (policy.RequiredLicense is not null && CatalogJson.GetString(json, "license") != policy.RequiredLicense)
            errors.Add($"{description} must declare license {policy.RequiredLicense}.");
        if (policy.RequiredAuthorName is not null &&
            (json["author"] is not JsonObject author || CatalogJson.GetString(author, "name") != policy.RequiredAuthorName))
            errors.Add($"{description} must declare author {policy.RequiredAuthorName}.");
    }

    private static void ScanUrls(string package, string file, string name, ValidationPolicy policy, List<string> errors)
    {
        var info = new FileInfo(file);
        if (info.Length > MaxScannedFileBytes) return;
        var bytes = File.ReadAllBytes(file);
        if (Array.IndexOf(bytes, (byte)0) >= 0) return;
        var lines = Encoding.UTF8.GetString(bytes).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            foreach (Match match in Url().Matches(lines[index]))
            {
                var url = match.Value.TrimEnd('.', ',', ';', ':');
                if (policy.ForbiddenUrlPrefixes.Any(prefix => url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) &&
                    !policy.AllowedUrlPrefixes.Any(prefix => MatchesPrefix(url, prefix)))
                    errors.Add($"Plugin {name} references a disallowed URL at {SafePaths.ToPortableRelative(package, file)}:{index + 1}: {url}");
            }
        }
    }

    private static bool MatchesPrefix(string url, string prefix) =>
        url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
        (url.Length == prefix.Length || "/#?".Contains(url[prefix.Length]));

    [GeneratedRegex(@"https?://[^\s)<>""'`\]]+")]
    private static partial Regex Url();
}
