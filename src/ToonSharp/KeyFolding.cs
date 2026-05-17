using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ToonSharp;

internal static class KeyFolding
{
    private static readonly Regex FoldablePathSegmentRegex =
        new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static bool IsFoldableSegment(string segment) => FoldablePathSegmentRegex.IsMatch(segment);

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
        _ = flattenDepth;
        return new HashSet<string>(mapping.Keys, StringComparer.Ordinal);
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