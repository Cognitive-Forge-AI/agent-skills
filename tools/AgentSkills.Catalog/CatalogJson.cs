using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Consistent JSON reading and writing for catalogs and manifests.</summary>
public static class CatalogJson
{
    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonObject ReadObject(string path)
    {
        SafePaths.RejectLinks(path);
        return ParseObject(File.ReadAllText(path), path);
    }

    public static JsonObject ParseObject(string text, string description)
    {
        try
        {
            return JsonNode.Parse(text) as JsonObject ?? throw new CatalogException($"{description} must contain a JSON object.");
        }
        catch (JsonException error)
        {
            throw new CatalogException($"{description} is not valid JSON: {error.Message}");
        }
    }

    public static string Serialize(JsonNode node) => node.ToJsonString(WriteOptions) + "\n";

    public static byte[] ToBytes(JsonNode node) => Encoding.UTF8.GetBytes(Serialize(node));

    public static void Write(string path, JsonNode node)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, ToBytes(node));
    }

    public static string? GetString(JsonObject json, string key) =>
        json[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Returns string values from a string-or-array component declaration.</summary>
    public static IEnumerable<string> StringValues(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var single))
        {
            yield return single;
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                if (item is JsonValue itemValue && itemValue.TryGetValue<string>(out var text)) yield return text;
        }
    }
}
