using System.Text.Json;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Repository-specific rules applied on top of structural catalog validation.</summary>
public sealed record ValidationPolicy
{
    public const string FileName = "catalog-policy.json";

    /// <summary>SPDX identifier every catalog entry and package manifest must declare, with a license file in the package.</summary>
    public string? RequiredLicense { get; init; }

    /// <summary>Author name every catalog entry and package manifest must declare.</summary>
    public string? RequiredAuthorName { get; init; }

    public IReadOnlyList<string> LicenseFiles { get; init; } = ["LICENSE", "LICENSE.md", "LICENSE.txt"];

    public bool RequireCursorManifest { get; init; } = true;

    /// <summary>Every directory under <c>plugins/</c> must be a catalog package.</summary>
    public bool RequireReferencedPackages { get; init; } = true;

    /// <summary>URL prefixes that package text must not reference, such as private organization repositories.</summary>
    public IReadOnlyList<string> ForbiddenUrlPrefixes { get; init; } = [];

    /// <summary>Exceptions to <see cref="ForbiddenUrlPrefixes"/>, matched as a whole path prefix.</summary>
    public IReadOnlyList<string> AllowedUrlPrefixes { get; init; } = [];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>Loads <c>catalog-policy.json</c> from a marketplace root, or returns the default policy when absent.</summary>
    public static ValidationPolicy LoadOrDefault(string marketplaceRoot)
    {
        var path = Path.Combine(marketplaceRoot, FileName);
        if (!File.Exists(path)) return new ValidationPolicy();
        SafePaths.RejectLinks(path);
        try
        {
            return JsonSerializer.Deserialize<ValidationPolicy>(File.ReadAllText(path), Options)
                ?? throw new CatalogException($"{FileName} must contain a JSON object.");
        }
        catch (JsonException error)
        {
            throw new CatalogException($"{FileName} is invalid: {error.Message}");
        }
    }
}
