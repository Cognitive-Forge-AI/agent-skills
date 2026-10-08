using System.Text.Json.Nodes;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Per-package manifest locations and the Cursor manifest derivation.</summary>
public static class PackageManifests
{
    public const string ClaudePath = ".claude-plugin/plugin.json";
    public const string CodexPath = ".codex-plugin/plugin.json";
    public const string CursorPath = ".cursor-plugin/plugin.json";

    public static IReadOnlyList<string> All { get; } = [ClaudePath, CodexPath, CursorPath];

    /// <summary>Manifest keys whose values reference files inside the package.</summary>
    public static IReadOnlyList<string> ComponentKeys { get; } = ["skills", "commands", "agents", "hooks", "mcpServers", "apps", "rules"];

    private static readonly string[] CursorCopiedKeys =
        ["name", "displayName", "description", "version", "homepage", "repository", "license", "logo", "keywords", "category", "tags",
         "commands", "agents", "skills", "rules", "hooks", "mcpServers"];

    /// <summary>
    /// Derives the Cursor manifest from a common plugin manifest (the Claude-format manifest).
    /// Cursor's schema forbids unknown properties, so only documented fields are copied.
    /// </summary>
    public static JsonObject CreateCursor(JsonObject common, string? displayName = null, string? category = null, IEnumerable<string>? tags = null)
    {
        var source = (JsonObject)common.DeepClone();
        if (displayName is not null) source["displayName"] = displayName;
        if (category is not null) source["category"] = category;
        if (tags is not null && tags.Any()) source["tags"] = new JsonArray(tags.Select(tag => (JsonNode?)JsonValue.Create(tag)).ToArray());
        var cursor = new JsonObject();
        foreach (var key in CursorCopiedKeys)
            if (source[key] is { } value) cursor[key] = value.DeepClone();
        if (source["author"] is JsonObject author && CatalogJson.GetString(author, "name") is { } name)
        {
            var cursorAuthor = new JsonObject { ["name"] = name };
            if (CatalogJson.GetString(author, "email") is { } email) cursorAuthor["email"] = email;
            // Preserve declared field order: author follows version in the documented layout.
            cursor = Insert(cursor, "author", cursorAuthor, after: "version");
        }
        return cursor;
    }

    private static JsonObject Insert(JsonObject json, string key, JsonNode value, string after)
    {
        var result = new JsonObject();
        var inserted = false;
        foreach (var (existingKey, existingValue) in json)
        {
            result[existingKey] = existingValue?.DeepClone();
            if (existingKey == after)
            {
                result[key] = value;
                inserted = true;
            }
        }
        if (!inserted) result[key] = value;
        return result;
    }
}
