namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Relative-path validation and link rejection for package content.</summary>
public static class SafePaths
{
    /// <summary>Rejects reparse points in the path and every ancestor; a regular leaf can still resolve through a linked parent.</summary>
    public static void RejectLinks(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Directory.GetParent(current)?.FullName)
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new CatalogException($"Links are not allowed in package content: {current}");
        }
    }

    /// <summary>Enumerates regular files below a root, rejecting links before they are read.</summary>
    public static IEnumerable<string> EnumerateFiles(string root)
    {
        RejectLinks(root);
        return Enumerate(root);

        static IEnumerable<string> Enumerate(string directory)
        {
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new CatalogException($"Links are not allowed in package content: {path}");
                yield return path;
            }
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                    throw new CatalogException($"Links are not allowed in package content: {child}");
                foreach (var path in Enumerate(child)) yield return path;
            }
        }
    }

    public static bool IsUnderRoot(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == ".") return true;
        return !Path.IsPathRooted(relative) &&
            relative != ".." &&
            !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
            !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    /// <summary>Validates a manifest-relative path: no roots, drives, UNC prefixes, empty or parent segments.</summary>
    public static void ValidateRelativePath(string path, string context, string rootDescription = "package root")
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
            Path.IsPathRooted(path) ||
            (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':') ||
            path.StartsWith('\\') ||
            path.StartsWith('/'))
            throw new CatalogException($"{context} must be a safe relative path.");

        var relative = path;
        while (relative.StartsWith("./", StringComparison.Ordinal) || relative.StartsWith(@".\", StringComparison.Ordinal))
            relative = relative[2..];
        relative = relative.TrimEnd('/', '\\');
        if (relative.Length == 0) throw new CatalogException($"{context} must be a safe relative path.");

        foreach (var segment in relative.Split(['/', '\\']))
        {
            if (segment.Length == 0 || segment == ".") throw new CatalogException($"{context} must be a safe relative path.");
            if (segment == "..") throw new CatalogException($"Path escapes {rootDescription}: {path}");
        }
    }

    public static string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        while (normalized.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            normalized = normalized[2..];
        return normalized.TrimStart(Path.DirectorySeparatorChar);
    }

    /// <summary>Resolves a validated relative path below a root.</summary>
    public static string Resolve(string root, string relativePath, string context, string rootDescription = "package root")
    {
        ValidateRelativePath(relativePath, context, rootDescription);
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, NormalizeRelativePath(relativePath)));
        if (!IsUnderRoot(fullRoot, fullPath)) throw new CatalogException($"Path escapes {rootDescription}: {relativePath}");
        return fullPath;
    }

    /// <summary>Formats a relative path as a ./-prefixed plugin reference with forward slashes.</summary>
    public static string? ToPluginReference(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : "./" + NormalizeRelativePath(path).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>Returns the root-relative path with forward slashes.</summary>
    public static string ToPortableRelative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
