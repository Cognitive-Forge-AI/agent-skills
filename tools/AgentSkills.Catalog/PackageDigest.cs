using System.Security.Cryptography;
using System.Text;

namespace CognitiveForge.AgentSkills.Catalog;

/// <summary>Deterministic digests of package trees and in-memory package file sets.</summary>
public static class PackageDigest
{
    /// <summary>Digests the files below a root. The filter receives forward-slash relative paths.</summary>
    public static string Compute(string root, Func<string, bool>? include = null)
    {
        var files = SafePaths.EnumerateFiles(root)
            .Select(path => (Relative: SafePaths.ToPortableRelative(root, path), Path: path))
            .Where(file => include?.Invoke(file.Relative) ?? true)
            .ToDictionary(file => file.Relative, file => (Func<byte[]>)(() => File.ReadAllBytes(file.Path)), StringComparer.Ordinal);
        return Compute(files);
    }

    /// <summary>Digests relative paths and lazily read content in ordinal path order.</summary>
    public static string Compute(IReadOnlyDictionary<string, Func<byte[]>> files)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var relative in files.Keys.Order(StringComparer.Ordinal))
        {
            digest.AppendData(Encoding.UTF8.GetBytes(relative + "\0"));
            digest.AppendData(SHA256.HashData(files[relative]()));
        }
        return Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant();
    }

    public static string Compute(IReadOnlyDictionary<string, byte[]> files) =>
        Compute(files.ToDictionary(item => item.Key, item => (Func<byte[]>)(() => item.Value), StringComparer.Ordinal));
}
