using System.Text.RegularExpressions;

namespace CognitiveForge.AgentSkills.Catalog;

public sealed class CatalogException(string message) : Exception(message);

/// <summary>Identifier and version rules shared by catalogs and package manifests.</summary>
public static partial class Names
{
    public static bool IsPluginName(string? value) => value is not null && PluginName().IsMatch(value);

    public static bool IsVersion(string? value) => value is not null && Version().IsMatch(value);

    public static bool IsStableVersion(string? value) => value is not null && StableVersion().IsMatch(value);

    public static bool IsPackageDirectory(string? value) => value is not null && PackageDirectory().IsMatch(value);

    /// <summary>Returns the next minor version of a stable major.minor.patch version, resetting patch.</summary>
    public static string NextMinor(string version)
    {
        if (!IsStableVersion(version)) throw new CatalogException($"Marketplace version must be a stable major.minor.patch version: {version}");
        var parts = version.Split('.');
        return $"{int.Parse(parts[0])}.{int.Parse(parts[1]) + 1}.0";
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*\z")]
    private static partial Regex PluginName();

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?\z")]
    private static partial Regex Version();

    [GeneratedRegex(@"^\d+\.\d+\.\d+\z")]
    private static partial Regex StableVersion();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]*\z")]
    private static partial Regex PackageDirectory();
}
