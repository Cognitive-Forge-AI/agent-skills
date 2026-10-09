using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using CognitiveForge.AgentSkills.Catalog;

/// <summary>A temporary marketplace checkout with one valid package.</summary>
internal sealed class MarketplaceFixture : IDisposable
{
    public MarketplaceFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "agent-skills-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Catalog = new JsonObject
        {
            ["$schema"] = "https://anthropic.com/claude-code/marketplace.schema.json",
            ["name"] = "agent-skills",
            ["description"] = "An open marketplace for agent plugins and skills.",
            ["version"] = "0.1.0",
            ["owner"] = new JsonObject { ["name"] = "Cognitive Forge", ["email"] = "opensource@cognitiveforge.ai" },
            ["plugins"] = new JsonArray()
        };
    }

    public string Root { get; }

    public JsonObject Catalog { get; }

    public static ValidationPolicy PublicPolicy { get; } = new()
    {
        RequiredLicense = "Apache-2.0",
        RequiredAuthorName = "Cognitive Forge",
        RequiredPackageFiles = ["NOTICE"],
        RequireSkillLicense = true,
        UrlScanExcludedPaths = ["manifest.json"],
        ForbiddenUrlPrefixes = ["https://github.com/Cognitive-Forge-AI/"],
        AllowedUrlPrefixes = ["https://github.com/Cognitive-Forge-AI/agent-skills"]
    };

    public string Package(string directory) => Path.Combine(Root, "plugins", directory);

    public void AddPackage(string name, string version = "1.0.0", string? directory = null)
    {
        directory ??= name;
        var common = new JsonObject
        {
            ["name"] = name,
            ["version"] = version,
            ["description"] = $"{name} skills",
            ["skills"] = "./skills/",
            ["author"] = new JsonObject { ["name"] = "Cognitive Forge", ["email"] = "opensource@cognitiveforge.ai" },
            ["homepage"] = $"https://github.com/Cognitive-Forge-AI/agent-skills/tree/main/plugins/{directory}",
            ["repository"] = "https://github.com/Cognitive-Forge-AI/agent-skills",
            ["license"] = "Apache-2.0"
        };
        var package = Package(directory);
        Write(Path.Combine(package, "skills", "sample", "SKILL.md"), "---\nname: sample\ndescription: Sample\nlicense: Apache-2.0\n---\nSample.");
        Write(Path.Combine(package, "LICENSE"), "Apache License 2.0");
        Write(Path.Combine(package, "NOTICE"), "Sample\nCopyright 2026 Cognitive Forge Inc.");
        CatalogJson.Write(Path.Combine(package, PackageManifests.ClaudePath), common);
        var codex = (JsonObject)common.DeepClone();
        codex["interface"] = new JsonObject { ["displayName"] = name, ["developerName"] = "Cognitive Forge" };
        CatalogJson.Write(Path.Combine(package, PackageManifests.CodexPath), codex);
        CatalogJson.Write(Path.Combine(package, PackageManifests.CursorPath), PackageManifests.CreateCursor(common, category: "productivity"));
        Catalog["plugins"]!.AsArray().Add(new JsonObject
        {
            ["name"] = name,
            ["description"] = $"{name} skills",
            ["version"] = version,
            ["author"] = new JsonObject { ["name"] = "Cognitive Forge", ["email"] = "opensource@cognitiveforge.ai" },
            ["license"] = "Apache-2.0",
            ["category"] = "productivity",
            ["source"] = $"./plugins/{directory}",
            ["strict"] = true,
            ["skills"] = new JsonArray("./skills/")
        });
    }

    public void Save()
    {
        CatalogJson.Write(Path.Combine(Root, CatalogViews.RootPath), Catalog);
        CatalogViews.WriteAll(Root, Catalog);
    }

    public static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    public static bool TryCreateDirectoryLink(string linkPath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var start = new ProcessStartInfo("cmd") { RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { "/c", "mklink", "/J", linkPath, targetPath }) start.ArgumentList.Add(argument);
                using var process = Process.Start(start)!;
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(Root)) Delete(Root);

        static void Delete(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                Directory.Delete(path);
                return;
            }
            foreach (var file in Directory.EnumerateFiles(path)) File.Delete(file);
            foreach (var directory in Directory.EnumerateDirectories(path)) Delete(directory);
            Directory.Delete(path);
        }
    }
}
