using System.Text.Json.Nodes;
using CognitiveForge.AgentSkills.Catalog;
using Xunit;

public sealed class CatalogViewTests
{
    private static JsonObject Root() => JsonNode.Parse("""
        {
          "$schema": "https://anthropic.com/claude-code/marketplace.schema.json",
          "name": "agent-skills",
          "description": "An open marketplace for agent plugins and skills.",
          "version": "0.3.0",
          "owner": { "name": "Cognitive Forge", "email": "opensource@cognitiveforge.ai", "url": "https://example.com" },
          "plugins": [
            { "name": "blueprints", "description": "Project memory", "version": "0.2.0-preview.1", "category": "productivity",
              "source": "./plugins/blueprints", "strict": true, "skills": ["./skills/"], "license": "Apache-2.0" },
            { "name": "no-category", "version": "1.0.0", "source": "./plugins/no-category" }
          ]
        }
        """)!.AsObject();

    [Fact]
    public void Claude_view_is_the_root_catalog()
    {
        var root = Root();
        Assert.True(JsonNode.DeepEquals(root, CatalogViews.CreateClaude(root)));
    }

    [Fact]
    public void Codex_view_uses_local_sources_policy_and_category_without_schema()
    {
        var codex = CatalogViews.CreateCodex(Root());
        Assert.False(codex.ContainsKey("$schema"));
        var first = codex["plugins"]![0]!;
        Assert.Equal("local", (string?)first["source"]!["source"]);
        Assert.Equal("./plugins/blueprints", (string?)first["source"]!["path"]);
        Assert.Equal("AVAILABLE", (string?)first["policy"]!["installation"]);
        Assert.Equal("ON_INSTALL", (string?)first["policy"]!["authentication"]);
        Assert.Equal(CatalogViews.DefaultCodexCategory, (string?)codex["plugins"]![1]!["category"]);
        Assert.Equal("0.3.0", (string?)codex["version"]);
    }

    [Fact]
    public void Cursor_view_emits_only_schema_fields()
    {
        var cursor = CatalogViews.CreateCursor(Root());
        Assert.Equal(new[] { "name", "owner", "metadata", "plugins" }, cursor.Select(item => item.Key));
        Assert.Equal(new[] { "name", "email" }, cursor["owner"]!.AsObject().Select(item => item.Key));
        Assert.Equal("0.3.0", (string?)cursor["metadata"]!["version"]);
        var first = cursor["plugins"]![0]!.AsObject();
        Assert.Equal(new[] { "name", "source", "description" }, first.Select(item => item.Key));
        Assert.Equal("plugins/blueprints", (string?)first["source"]);
        Assert.Equal(new[] { "name", "source" }, cursor["plugins"]![1]!.AsObject().Select(item => item.Key));
    }

    [Fact]
    public void Cursor_manifest_keeps_documented_fields_only()
    {
        var common = JsonNode.Parse("""
            { "name": "sample", "version": "1.0.0", "description": "Sample", "skills": "./skills/",
              "author": { "name": "Cognitive Forge", "email": "opensource@cognitiveforge.ai", "url": "https://example.com" },
              "license": "Apache-2.0", "apps": "./.app.json", "interface": { "displayName": "Sample" },
              "agents": ["./agents/reviewer.md"], "hooks": "./hooks/hooks.json" }
            """)!.AsObject();
        var cursor = PackageManifests.CreateCursor(common, "Sample", "productivity", ["review"]);
        Assert.False(cursor.ContainsKey("apps"));
        Assert.False(cursor.ContainsKey("interface"));
        Assert.Equal(new[] { "name", "email" }, cursor["author"]!.AsObject().Select(item => item.Key));
        Assert.Equal("Sample", (string?)cursor["displayName"]);
        Assert.Equal("productivity", (string?)cursor["category"]);
        Assert.Equal("review", (string?)cursor["tags"]![0]);
        Assert.Equal("./agents/reviewer.md", (string?)cursor["agents"]![0]);
        Assert.Equal(new[] { "name", "displayName", "description", "version", "author" }, cursor.Select(item => item.Key).Take(5));
    }

    [Fact]
    public void Views_are_written_with_a_trailing_newline()
    {
        using var fixture = new MarketplaceFixture();
        fixture.Save();
        foreach (var view in CatalogViews.ViewPaths)
            Assert.EndsWith("}\n", File.ReadAllText(Path.Combine(fixture.Root, view)));
    }
}

public sealed class CatalogValidatorTests
{
    [Fact]
    public void Empty_bootstrap_catalog_is_valid()
    {
        using var fixture = new MarketplaceFixture();
        fixture.Save();
        Assert.Empty(CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy));
    }

    [Fact]
    public void Valid_package_passes_the_public_policy()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        Assert.Empty(CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy));
    }

    [Fact]
    public void Out_of_date_view_is_reported()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        fixture.Catalog["version"] = "0.2.0";
        CatalogJson.Write(Path.Combine(fixture.Root, CatalogViews.RootPath), fixture.Catalog);
        var errors = CatalogValidator.Validate(fixture.Root);
        Assert.Equal(3, errors.Count(error => error.Contains("regenerate views")));
    }

    [Fact]
    public void Missing_view_and_manifest_mismatch_are_reported()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        File.Delete(Path.Combine(fixture.Root, CatalogViews.CursorPath));
        var codex = CatalogJson.ReadObject(Path.Combine(fixture.Package("sample"), PackageManifests.CodexPath));
        codex["version"] = "9.9.9";
        CatalogJson.Write(Path.Combine(fixture.Package("sample"), PackageManifests.CodexPath), codex);
        var errors = CatalogValidator.Validate(fixture.Root);
        Assert.Contains(errors, error => error.Contains(".cursor-plugin/marketplace.json is missing"));
        Assert.Contains(errors, error => error.Contains(".codex-plugin/plugin.json name/version does not match"));
    }

    [Fact]
    public void Missing_declared_component_is_reported()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        Directory.Delete(Path.Combine(fixture.Package("sample"), "skills"), recursive: true);
        Assert.Contains(CatalogValidator.Validate(fixture.Root), error => error.Contains("declares missing skills component"));
    }

    [Fact]
    public void Duplicate_names_shared_directories_and_orphans_are_reported()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Catalog["plugins"]!.AsArray().Add(fixture.Catalog["plugins"]![0]!.DeepClone());
        fixture.Save();
        Directory.CreateDirectory(fixture.Package("orphan"));
        var errors = CatalogValidator.Validate(fixture.Root);
        Assert.Contains(errors, error => error.Contains("Duplicate plugin name: sample"));
        Assert.Contains(errors, error => error.Contains("shares package directory"));
        Assert.Contains(errors, error => error.Contains("plugins/orphan is not referenced"));
    }

    [Fact]
    public void Unsafe_source_is_rejected()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Catalog["plugins"]![0]!["source"] = "./plugins/../outside";
        fixture.Save();
        Assert.Contains(CatalogValidator.Validate(fixture.Root), error => error.Contains("Plugin source must be ./plugins/{directory}"));
    }

    [Fact]
    public void License_and_author_policy_are_enforced()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Catalog["plugins"]![0]!["license"] = "MIT";
        fixture.Catalog["plugins"]![0]!["author"] = new JsonObject { ["name"] = "Someone" };
        fixture.Save();
        File.Delete(Path.Combine(fixture.Package("sample"), "LICENSE"));
        var errors = CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy);
        Assert.Contains(errors, error => error.Contains("Catalog entry sample must declare license Apache-2.0"));
        Assert.Contains(errors, error => error.Contains("Catalog entry sample must declare author Cognitive Forge"));
        Assert.Contains(errors, error => error.Contains("must include a license file"));
    }

    [Fact]
    public void Private_repository_links_are_rejected_but_public_marketplace_links_are_allowed()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        MarketplaceFixture.Write(Path.Combine(fixture.Package("sample"), "skills", "sample", "notes.md"),
            "See [issue](https://github.com/Cognitive-Forge-AI/blueprints/issues/11).\n" +
            "Public: https://github.com/Cognitive-Forge-AI/agent-skills/tree/main/plugins/sample\n" +
            "Lookalike: https://github.com/Cognitive-Forge-AI/agent-skills-private");
        var errors = CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy);
        Assert.Contains(errors, error => error.Contains("skills/sample/notes.md:1") && error.Contains("blueprints/issues/11"));
        Assert.Contains(errors, error => error.Contains("notes.md:3") && error.Contains("agent-skills-private"));
        Assert.DoesNotContain(errors, error => error.Contains("notes.md:2"));
    }

    [Fact]
    public void Notice_and_skill_license_policy_are_enforced()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        File.Delete(Path.Combine(fixture.Package("sample"), "NOTICE"));
        MarketplaceFixture.Write(Path.Combine(fixture.Package("sample"), "skills", "unlicensed", "SKILL.md"), "---\nname: unlicensed\ndescription: No license\n---\nBody.");
        MarketplaceFixture.Write(Path.Combine(fixture.Package("sample"), "skills", "other", "SKILL.md"), "---\nname: other\nlicense: \"MIT\"\n---\nBody.");
        var errors = CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy);
        Assert.Contains(errors, error => error.Contains("must include NOTICE at its root"));
        Assert.Contains(errors, error => error.Contains("skills/unlicensed/SKILL.md must declare license in its frontmatter"));
        Assert.Contains(errors, error => error.Contains("skills/other/SKILL.md must declare license Apache-2.0, not MIT"));
        Assert.DoesNotContain(errors, error => error.Contains("skills/sample/SKILL.md"));
    }

    [Fact]
    public void Url_scan_skips_excluded_paths()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        MarketplaceFixture.Write(Path.Combine(fixture.Package("sample"), "manifest.json"), "{\"homepage\":\"https://github.com/Cognitive-Forge-AI/private-spoke\"}");
        Assert.Empty(CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy));
        MarketplaceFixture.Write(Path.Combine(fixture.Package("sample"), "notes.md"), "https://github.com/Cognitive-Forge-AI/private-spoke");
        Assert.Contains(CatalogValidator.Validate(fixture.Root, MarketplaceFixture.PublicPolicy), error => error.Contains("notes.md:1"));
    }

    [Fact]
    public void Package_without_components_is_rejected()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        foreach (var manifestPath in PackageManifests.All)
        {
            var path = Path.Combine(fixture.Package("sample"), manifestPath);
            var manifest = CatalogJson.ReadObject(path);
            manifest.Remove("skills");
            CatalogJson.Write(path, manifest);
        }
        Assert.Contains(CatalogValidator.Validate(fixture.Root), error => error.Contains("declares no components"));
    }

    [Theory]
    [InlineData("---\nname: a\nlicense: Apache-2.0\n---\nbody", "Apache-2.0")]
    [InlineData("---\r\nlicense: 'MIT'\r\n---\r\nbody", "MIT")]
    [InlineData("---\nmetadata:\n  license: nested\n---\nbody", null)]
    [InlineData("no frontmatter\nlicense: Apache-2.0", null)]
    public void Frontmatter_values_are_read_from_top_level_keys(string markdown, string? expected) =>
        Assert.Equal(expected, CatalogValidator.FrontmatterValue(markdown, "license"));

    [Fact]
    public void Linked_package_content_is_rejected()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Save();
        var outside = Path.Combine(fixture.Root, "outside");
        MarketplaceFixture.Write(Path.Combine(outside, "SKILL.md"), "private");
        Assert.True(MarketplaceFixture.TryCreateDirectoryLink(Path.Combine(fixture.Package("sample"), "skills", "linked"), outside));
        Assert.Contains(CatalogValidator.Validate(fixture.Root), error => error.Contains("Links are not allowed"));
    }

    [Fact]
    public void Policy_file_is_loaded_and_unknown_fields_are_rejected()
    {
        using var fixture = new MarketplaceFixture();
        MarketplaceFixture.Write(Path.Combine(fixture.Root, ValidationPolicy.FileName), """{ "requiredLicense": "Apache-2.0", "forbiddenUrlPrefixes": ["https://x/"] }""");
        var policy = ValidationPolicy.LoadOrDefault(fixture.Root);
        Assert.Equal("Apache-2.0", policy.RequiredLicense);
        Assert.Equal(["https://x/"], policy.ForbiddenUrlPrefixes);
        MarketplaceFixture.Write(Path.Combine(fixture.Root, ValidationPolicy.FileName), """{ "requiredLicence": "Apache-2.0" }""");
        Assert.Throws<CatalogException>(() => ValidationPolicy.LoadOrDefault(fixture.Root));
    }
}

public sealed class PathAndDigestTests
{
    [Theory]
    [InlineData("../outside", "Path escapes")]
    [InlineData("skills/../../outside", "Path escapes")]
    [InlineData("/rooted", "must be a safe relative path")]
    [InlineData("C:/rooted", "must be a safe relative path")]
    [InlineData("./", "must be a safe relative path")]
    [InlineData("a//b", "must be a safe relative path")]
    public void Unsafe_relative_paths_are_rejected(string path, string expected)
    {
        var error = Assert.Throws<CatalogException>(() => SafePaths.ValidateRelativePath(path, "test path"));
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Plugin_references_are_normalized()
    {
        Assert.Equal("./hooks/hooks.json", SafePaths.ToPluginReference(@".\hooks\hooks.json"));
        Assert.Null(SafePaths.ToPluginReference(" "));
    }

    [Fact]
    public void Digest_depends_on_paths_and_content_but_not_insertion_order()
    {
        var first = PackageDigest.Compute(new Dictionary<string, byte[]> { ["a.txt"] = [1], ["b/c.txt"] = [2] });
        var reordered = PackageDigest.Compute(new Dictionary<string, byte[]> { ["b/c.txt"] = [2], ["a.txt"] = [1] });
        var changed = PackageDigest.Compute(new Dictionary<string, byte[]> { ["a.txt"] = [1], ["b/c.txt"] = [3] });
        var moved = PackageDigest.Compute(new Dictionary<string, byte[]> { ["a.txt"] = [1], ["b/d.txt"] = [2] });
        Assert.Equal(first, reordered);
        Assert.NotEqual(first, changed);
        Assert.NotEqual(first, moved);
    }

    [Fact]
    public void Directory_digest_matches_in_memory_digest_and_honors_filter()
    {
        using var fixture = new MarketplaceFixture();
        MarketplaceFixture.Write(Path.Combine(fixture.Root, "pkg", "a.txt"), "a");
        MarketplaceFixture.Write(Path.Combine(fixture.Root, "pkg", "manifest.json"), "{}");
        var expected = PackageDigest.Compute(new Dictionary<string, byte[]> { ["a.txt"] = "a"u8.ToArray() });
        Assert.Equal(expected, PackageDigest.Compute(Path.Combine(fixture.Root, "pkg"), relative => relative != "manifest.json"));
    }

    [Theory]
    [InlineData("0.1.0", "0.2.0")]
    [InlineData("1.9.3", "1.10.0")]
    public void Next_minor_resets_patch(string version, string expected) => Assert.Equal(expected, Names.NextMinor(version));

    [Theory]
    [InlineData("sample", true)]
    [InlineData("sample\n", false)]
    [InlineData("Sample", false)]
    [InlineData("double--hyphen", false)]
    public void Plugin_names_are_kebab_case(string name, bool valid) => Assert.Equal(valid, Names.IsPluginName(name));
}
