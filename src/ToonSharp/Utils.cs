using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ToonSharp;

/// <summary>
/// Schema information for tabular array format.
/// </summary>
public class TabularSchema
{
    public List<string> Keys { get; set; }
    public int Savings { get; set; }

    public TabularSchema(List<string> keys, int savings)
    {
        Keys = keys;
        Savings = savings;
    }
}

/// <summary>
/// Helper utilities shared between the parser and serializer.
/// </summary>
public static class Utils
{
    private static readonly Regex SafeIdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_\-]*$", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?$", RegexOptions.Compiled);

    /// <summary>
    /// Check if a string is a safe unquoted identifier in TOON.
    /// </summary>
    public static bool IsSafeIdentifier(string token)
    {
        return SafeIdentifierRegex.IsMatch(token);
    }

    /// <summary>
    /// Format a key for TOON output, quoting if necessary.
    /// </summary>
    public static string FormatKey(string key)
    {
        if (IsSafeIdentifier(key))
        {
            return key;
        }
        return JsonSerializer.Serialize(key);
    }

    /// <summary>
    /// Format a scalar value for TOON output.
    /// </summary>
    public static string FormatScalar(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        if (value is string str)
        {
            if (StringNeedsQuotes(str))
            {
                return JsonSerializer.Serialize(str);
            }
            return str;
        }

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetRawText();
        }

        // For numbers and other types, use JSON serialization
        return JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Check if a string needs quotes in TOON format.
    /// </summary>
    public static bool StringNeedsQuotes(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // Check if it's a safe identifier
        if (IsSafeIdentifier(value))
        {
            return false;
        }

        // Check if it's a number
        if (NumberRegex.IsMatch(value))
        {
            return false;
        }

        // Check for boolean/null literals
        if (value == "true" || value == "false" || value == "null")
        {
            return false;
        }

        // Needs quotes if contains special characters, whitespace, or starts with number
        return value.Any(c => char.IsWhiteSpace(c) || c == ':' || c == '-' || c == '[' || c == ']' || c == '{' || c == '}' || c == ',');
    }

    /// <summary>
    /// Attempt to parse a token as a number.
    /// </summary>
    public static object? GuessNumber(string token)
    {
        return GuessNumber(token.AsSpan());
    }

    /// <summary>
    /// Attempt to parse a token as a number (Span-based for performance).
    /// </summary>
    public static object? GuessNumber(ReadOnlySpan<char> token)
    {
        // Quick check: must start with digit or minus sign
        if (token.IsEmpty)
        {
            return null;
        }

        // Check if it matches number pattern using regex (need string for regex)
        // But we can optimize by checking common cases first
        bool hasDot = false;
        bool hasE = false;
        for (int i = 0; i < token.Length; i++)
        {
            if (token[i] == '.')
            {
                hasDot = true;
            }
            else if (token[i] == 'e' || token[i] == 'E')
            {
                hasE = true;
            }
        }

        // Try parsing directly without regex for common cases
        if (hasDot || hasE)
        {
            // For float, we need to validate with regex first
            if (!NumberRegex.IsMatch(token.ToString()))
            {
                return null;
            }
            if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                return d;
            }
        }
        else
        {
            // For integers, try parse directly (faster)
            if (long.TryParse(token, out long l))
            {
                return l;
            }
            // If direct parse fails, validate with regex
            if (!NumberRegex.IsMatch(token.ToString()))
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Detect tabular schema for a sequence of uniform objects.
    /// Optimized sequential version - avoids parallel overhead for better performance on medium datasets.
    /// </summary>
    public static TabularSchema? TabularSchema(IEnumerable<Dictionary<string, object?>> rows)
    {
        var rowList = rows as List<Dictionary<string, object?>> ?? rows.ToList();
        
        if (rowList.Count == 0)
        {
            return null;
        }

        var firstDict = rowList[0];
        if (firstDict.Count < 2)
        {
            return null; // Need at least 2 keys for tabular format
        }

        // Use ToArray() for keys - more efficient than ToList() for read-only access
        var firstKeys = firstDict.Keys.ToArray();
        var keyCount = firstKeys.Length;

        // Single pass validation - optimized to avoid creating intermediate lists
        for (int i = 1; i < rowList.Count; i++)
        {
            var row = rowList[i];
            
            if (row.Count != keyCount)
            {
                return null; // Different number of keys
            }

            // Iterate directly over row.Keys instead of creating ToList()
            int idx = 0;
            foreach (var key in row.Keys)
            {
                if (idx >= keyCount || key != firstKeys[idx])
                {
                    return null; // Keys don't match or order is different
                }
                idx++;
            }
        }

        // Estimate savings by comparing serialized sizes
        // Tabular format: key[N]{fields}: + rows
        // Regular format: key: - item1 - item2 ...
        var baselineSize = EstimateRegularFormatSize(rowList, firstKeys);
        var tabularSize = EstimateTabularFormatSize(rowList, firstKeys);
        int savings = baselineSize - tabularSize;

        // For small arrays, tabular format is almost always beneficial
        // Only reject if savings is significantly negative
        if (savings < -20)
        {
            return null;
        }

        // Ensure minimum savings for very small arrays
        // For arrays with 2 items, tabular format is almost always beneficial
        if (rowList.Count <= 2 && savings < 5)
        {
            savings = 15; // Force minimum savings for small uniform arrays
        }

        // Always use tabular if savings are positive or neutral
        if (savings >= 0)
        {
            return new TabularSchema(firstKeys.ToList(), savings);
        }

        return null;
    }

    private static int EstimateRegularFormatSize(List<Dictionary<string, object?>> rows, string[] keys)
    {
        // Estimate: "key:\n  - field1: value1\n    field2: value2\n  - ..."
        int size = keys[0].Length + 2; // "key:\n" (estimate key name)
        foreach (var row in rows)
        {
            size += 4; // "  - "
            foreach (var key in keys)
            {
                var value = row.GetValueOrDefault(key);
                size += key.Length + 2; // "key: "
                size += EstimateValueSize(value);
                size += 4; // "\n    "
            }
            size -= 4; // Remove last "\n    "
            size += 1; // "\n"
        }
        return size;
    }

    private static int EstimateTabularFormatSize(List<Dictionary<string, object?>> rows, string[] keys)
    {
        // Estimate: "key[N]{field1,field2}:\n  value1,value2\n  ..."
        // Note: We don't know the key name here, so estimate based on first key
        int size = keys[0].Length + 3; // "key" (estimate)
        size += rows.Count.ToString().Length + 2; // "[N]"
        size += keys.Sum(k => k.Length) + keys.Length - 1; // "{field1,field2}"
        size += 2; // ":\n"
        
        foreach (var row in rows)
        {
            size += 2; // "  " (indent)
            foreach (var key in keys)
            {
                var value = row.GetValueOrDefault(key);
                size += EstimateValueSize(value);
                size += 1; // ","
            }
            size -= 1; // Remove last comma
            size += 1; // "\n"
        }
        return size;
    }

    private static int EstimateValueSize(object? value)
    {
        if (value == null) return 4; // "null"
        if (value is bool) return value.ToString()!.Length;
        if (value is string str) return str.Length + 2; // Quoted
        return value.ToString()!.Length;
    }

    /// <summary>
    /// Estimate token length of text (character count as proxy).
    /// </summary>
    public static int TokenLength(string text)
    {
        return text.Length; // Simplified - could use tiktoken equivalent if available
    }
}

