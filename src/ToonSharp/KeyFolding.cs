using System.Collections.Generic;
using System.Linq;

namespace ToonSharp;

internal static class KeyFolding
{
    public static bool IsFoldableSegment(string segment) => Utils.IsSafeIdentifier(segment);

    public static string? TryGetFoldedPath(string key, object? value, int flattenDepth, out object? leaf)
    {
        leaf = null;
        if (flattenDepth < 2 || !IsFoldableSegment(key))
        {
            return null;
        }

        var segments = new List<string> { key };
        var current = value;

        while (current is Dictionary<string, object?> dict && dict.Count == 1)
        {
            if (segments.Count >= flattenDepth)
            {
                break;
            }

            var only = dict.First();
            if (!IsFoldableSegment(only.Key))
            {
                return null;
            }

            segments.Add(only.Key);
            current = only.Value;
        }

        if (segments.Count < 2)
        {
            return null;
        }

        leaf = current;
        return string.Join('.', segments);
    }

    public static HashSet<string> CollectLiteralAndFoldedKeys(Dictionary<string, object?> mapping, int flattenDepth)
    {
        var keys = new HashSet<string>(mapping.Keys, StringComparer.Ordinal);
        foreach (var kvp in mapping)
        {
            var folded = TryGetFoldedPath(kvp.Key, kvp.Value, flattenDepth, out _);
            if (folded != null)
            {
                keys.Add(folded);
            }
        }

        return keys;
    }

    public static bool WouldFoldCollide(string foldedPath, string sourceKey, HashSet<string> reservedKeys)
    {
        foreach (var other in reservedKeys)
        {
            if (other == sourceKey)
            {
                continue;
            }

            if (other == foldedPath ||
                other.StartsWith(foldedPath + ".", StringComparison.Ordinal) ||
                foldedPath.StartsWith(other + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}