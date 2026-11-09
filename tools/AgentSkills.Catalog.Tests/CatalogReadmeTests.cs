using System.Text.Json.Nodes;
using CognitiveForge.AgentSkills.Catalog;
using Xunit;

public sealed class CatalogReadmeTests
{
    private const string Start = CatalogReadme.StartMarker;
    private const string End = CatalogReadme.EndMarker;
    private const string StaleBlock = Start + "\nstale\n" + End;
    private const string Header = "| Plugin | What it provides | Current version |\n| --- | --- | --- |\n";

    private static string Generate(MarketplaceFixture fixture, string readme)
    {
        var path = Path.Combine(fixture.Root, CatalogReadme.ReadmePath);
        MarketplaceFixture.Write(path, readme);
        CatalogReadme.Write(fixture.Root, fixture.Catalog);
        return File.ReadAllText(path);
    }

    private static void EditCodexManifest(MarketplaceFixture fixture, string plugin, Action<JsonObject> edit)
    {
        var path = Path.Combine(fixture.Package(plugin), PackageManifests.CodexPath);
        var manifest = CatalogJson.ReadObject(path);
        edit(manifest);
        CatalogJson.Write(path, manifest);
    }

    [Fact]
    public void Fills_the_block_sorted_by_name_and_preserves_surrounding_text()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("zeta", "2.0.0");
        fixture.AddPackage("alpha", directory: "alpha-dir");

        var result = Generate(fixture, "# Title\r\n\nBefore.\n\n" + StaleBlock + "\n\nAfter.  \n");

        Assert.Equal(
            "# Title\r\n\nBefore.\n\n" + Start + "\n\n" + Header +
            "| [alpha](plugins/alpha-dir) | alpha skills | 1.0.0 |\n" +
            "| [zeta](plugins/zeta) | zeta skills | 2.0.0 |\n\n" + End + "\n\nAfter.  \n",
            result);
    }

    [Fact]
    public void Missing_readme_fails()
    {
        using var fixture = new MarketplaceFixture();
        Assert.Throws<CatalogException>(() => CatalogReadme.Write(fixture.Root, fixture.Catalog));
    }

    [Theory]
    [InlineData("No markers.")]
    [InlineData(Start)]
    [InlineData(End)]
    [InlineData(End + "\n" + Start)]
    [InlineData(Start + "\n" + Start + "\n" + End)]
    [InlineData(Start + "\n" + End + "\n" + End)]
    [InlineData(Start + "\n" + End + "\n" + Start + "\n" + End)]
    public void Anything_but_one_ordered_marker_pair_fails_and_leaves_the_readme_alone(string readme)
    {
        using var fixture = new MarketplaceFixture();
        var path = Path.Combine(fixture.Root, CatalogReadme.ReadmePath);
        MarketplaceFixture.Write(path, readme);

        Assert.Throws<CatalogException>(() => CatalogReadme.Write(fixture.Root, fixture.Catalog));
        Assert.Equal(readme, File.ReadAllText(path));
    }

    [Fact]
    public void Title_comes_from_the_codex_display_name()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        EditCodexManifest(fixture, "sample", manifest => manifest["interface"]!["displayName"] = "Sample Title");

        Assert.Contains("| [Sample Title](plugins/sample) |", Generate(fixture, StaleBlock));
    }

    [Fact]
    public void Title_falls_back_to_the_plugin_name()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("blank-title");
        fixture.AddPackage("no-interface");
        fixture.AddPackage("no-manifest");
        EditCodexManifest(fixture, "blank-title", manifest => manifest["interface"]!["displayName"] = "  ");
        EditCodexManifest(fixture, "no-interface", manifest => manifest.Remove("interface"));
        File.Delete(Path.Combine(fixture.Package("no-manifest"), PackageManifests.CodexPath));

        var result = Generate(fixture, StaleBlock);

        Assert.Contains("| [blank-title](plugins/blank-title) |", result);
        Assert.Contains("| [no-interface](plugins/no-interface) |", result);
        Assert.Contains("| [no-manifest](plugins/no-manifest) |", result);
    }

    [Fact]
    public void Hostile_values_stay_on_one_row_and_cannot_add_markers()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");
        fixture.Catalog["plugins"]![0]!["description"] = "a|b [x](y) \\ line\r\n\n  two " + End + " " + Start;
        EditCodexManifest(fixture, "sample", manifest => manifest["interface"]!["displayName"] = "Bad | [Name]\n<b>");

        var result = Generate(fixture, StaleBlock);

        var markers = End.Replace("<", "&lt;") + " " + Start.Replace("<", "&lt;");
        Assert.Contains($@"| [Bad \| \[Name\] &lt;b>](plugins/sample) | a\|b \[x\](y) \\ line two {markers} | 1.0.0 |" + "\n", result);
        CatalogReadme.Write(fixture.Root, fixture.Catalog);
        Assert.Equal(result, File.ReadAllText(Path.Combine(fixture.Root, CatalogReadme.ReadmePath)));
    }

    [Fact]
    public void Regenerating_is_idempotent()
    {
        using var fixture = new MarketplaceFixture();
        fixture.AddPackage("sample");

        var first = Generate(fixture, "Before.\n" + StaleBlock + "\nAfter.\n");

        Assert.Equal(first, Generate(fixture, first));
    }
}
