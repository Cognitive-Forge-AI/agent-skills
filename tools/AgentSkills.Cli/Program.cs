using CognitiveForge.AgentSkills.Catalog;

const string Usage = """
    Usage:
      agent-skills validate [--root <marketplace-root>]
      agent-skills views [--root <marketplace-root>]

    validate  Check the root catalog, packages, manifests, components, generated views, and catalog-policy.json.
    views     Regenerate the Claude Code, Codex, and Cursor catalog views from marketplace.json.
    """;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine(Usage);
    return args.Length == 0 ? 1 : 0;
}

var root = Directory.GetCurrentDirectory();
for (var index = 1; index < args.Length; index++)
{
    if (args[index] == "--root" && index + 1 < args.Length) root = args[++index];
    else
    {
        Console.Error.WriteLine($"Unknown argument: {args[index]}");
        Console.Error.WriteLine(Usage);
        return 1;
    }
}
root = Path.GetFullPath(root);

try
{
    switch (args[0])
    {
        case "validate":
            var errors = CatalogValidator.Validate(root, ValidationPolicy.LoadOrDefault(root));
            foreach (var error in errors) Console.Error.WriteLine($"error: {error}");
            if (errors.Count > 0) return 1;
            Console.WriteLine("Marketplace is valid.");
            return 0;
        case "views":
            CatalogViews.WriteAll(root, CatalogJson.ReadObject(Path.Combine(root, CatalogViews.RootPath)));
            Console.WriteLine($"Regenerated {string.Join(", ", CatalogViews.ViewPaths)}.");
            return 0;
        default:
            Console.Error.WriteLine($"Unknown command: {args[0]}");
            Console.Error.WriteLine(Usage);
            return 1;
    }
}
catch (Exception error) when (error is CatalogException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"error: {error.Message}");
    return 1;
}
