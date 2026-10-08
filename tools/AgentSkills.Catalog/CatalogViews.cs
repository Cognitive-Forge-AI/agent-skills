using System.Text.Json.Nodes;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>
/// Harness marketplace views generated from the authoritative root <c>marketplace.json</c>.
/// GitHub Copilot reads the root catalog directly; Claude Code, Codex, and Cursor read their own locations.
/// </summary>
public static class CatalogViews
{
    public const string RootPath = "marketplace.json";
    public const string ClaudePath = ".claude-plugin/marketplace.json";
    public const string CodexPath = ".agents/plugins/marketplace.json";
    public const string CursorPath = ".cursor-plugin/marketplace.json";

    /// <summary>Codex requires a category; entries without one use this documented default.</summary>
    public const string DefaultCodexCategory = "productivity";

    public static IReadOnlyList<string> ViewPaths { get; } = [ClaudePath, CodexPath, CursorPath];

    /// <summary>Creates every generated view keyed by its repository-relative path.</summary>
    public static IReadOnlyDictionary<string, JsonObject> Create(JsonObject root) => new Dictionary<string, JsonObject>(StringComparer.Ordinal)
    {
        [ClaudePath] = CreateClaude(root),
        [CodexPath] = CreateCodex(root),
        [CursorPath] = CreateCursor(root)
    };

    /// <summary>Claude Code reads the root catalog format unchanged.</summary>
    public static JsonObject CreateClaude(JsonObject root) => (JsonObject)root.DeepClone();

    /// <summary>Codex uses local source objects and requires installation policy and category.</summary>
    public static JsonObject CreateCodex(JsonObject root)
    {
        var view = (JsonObject)root.DeepClone();
        view.Remove("$schema");
        foreach (var plugin in Plugins(view))
        {
            if (CatalogJson.GetString(plugin, "source") is { } source)
                plugin["source"] = new JsonObject { ["source"] = "local", ["path"] = source };
            if (!plugin.ContainsKey("policy"))
                plugin["policy"] = new JsonObject { ["installation"] = "AVAILABLE", ["authentication"] = "ON_INSTALL" };
            if (!plugin.ContainsKey("category"))
                plugin["category"] = DefaultCodexCategory;
        }
        return view;
    }

    /// <summary>Cursor's schema forbids additional properties, so only its documented fields are emitted.</summary>
    public static JsonObject CreateCursor(JsonObject root)
    {
        var view = new JsonObject { ["name"] = CatalogJson.GetString(root, "name") };
        if (root["owner"] is JsonObject owner)
        {
            var cursorOwner = new JsonObject { ["name"] = CatalogJson.GetString(owner, "name") };
            if (CatalogJson.GetString(owner, "email") is { } email) cursorOwner["email"] = email;
            view["owner"] = cursorOwner;
        }
        var metadata = new JsonObject();
        if (CatalogJson.GetString(root, "description") is { } description) metadata["description"] = description;
        if (CatalogJson.GetString(root, "version") is { } version) metadata["version"] = version;
        if (metadata.Count > 0) view["metadata"] = metadata;
        var plugins = new JsonArray();
        foreach (var plugin in Plugins(root))
        {
            var entry = new JsonObject { ["name"] = CatalogJson.GetString(plugin, "name") };
            if (CatalogJson.GetString(plugin, "source") is { } source)
                entry["source"] = source.StartsWith("./", StringComparison.Ordinal) ? source[2..] : source;
            if (CatalogJson.GetString(plugin, "description") is { } pluginDescription) entry["description"] = pluginDescription;
            plugins.Add(entry);
        }
        view["plugins"] = plugins;
        return view;
    }

    /// <summary>Writes every generated view below a marketplace root.</summary>
    public static void WriteAll(string marketplaceRoot, JsonObject root)
    {
        foreach (var (relative, view) in Create(root))
        {
            var path = Path.Combine(marketplaceRoot, relative);
            var directory = Path.GetDirectoryName(path)!;
            if (Directory.Exists(directory)) SafePaths.RejectLinks(directory);
            if (File.Exists(path)) SafePaths.RejectLinks(path);
            CatalogJson.Write(path, view);
        }
    }

    public static IEnumerable<JsonObject> Plugins(JsonObject catalog) =>
        catalog["plugins"] is JsonArray array ? array.OfType<JsonObject>() : [];
}
