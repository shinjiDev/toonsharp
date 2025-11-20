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
    /// </summary>
    public static TabularSchema? TabularSchema(IEnumerable<Dictionary<string, object?>> rows)
    {
        var rowList = rows.ToList();
        if (rowList.Count == 0)
        {
            return null;
        }

        // Check if all items are dictionaries with the same keys
        var firstKeys = rowList[0].Keys.ToList();
        if (firstKeys.Count < 2)
        {
            return null; // Need at least 2 keys for tabular format
        }

        // Check that all rows have the same keys in the same order
        // Use parallel processing for large row sets (threshold: 100 rows)
        // Optimized based on benchmark results
        const int parallelThreshold = 100;
        var rowsToCheck = rowList.Skip(1).ToList();
        
        if (rowsToCheck.Count >= parallelThreshold)
        {
            // Parallel validation
            var isValid = true;
            var lockObj = new object();
            Parallel.ForEach(rowsToCheck, (row, state) =>
            {
                lock (lockObj)
                {
                    if (!isValid)
                    {
                        state.Stop();
                        return;
                    }
                }
                
                var rowKeys = row.Keys.ToList();
                if (rowKeys.Count != firstKeys.Count)
                {
                    lock (lockObj)
                    {
                        isValid = false;
                    }
                    state.Stop();
                    return;
                }
                
                // Check that keys match in order
                for (int i = 0; i < firstKeys.Count; i++)
                {
                    if (rowKeys[i] != firstKeys[i])
                    {
                        lock (lockObj)
                        {
                            isValid = false;
                        }
                        state.Stop();
                        return;
                    }
                }
            });
            
            if (!isValid)
            {
                return null;
            }
        }
        else
        {
            // Sequential validation for small sets
            foreach (var row in rowsToCheck)
            {
                var rowKeys = row.Keys.ToList();
                if (rowKeys.Count != firstKeys.Count)
                {
                    return null; // Different number of keys
                }
                
                // Check that keys match in order
                for (int i = 0; i < firstKeys.Count; i++)
                {
                    if (rowKeys[i] != firstKeys[i])
                    {
                        return null; // Keys don't match or order is different
                    }
                }
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
            return new TabularSchema(firstKeys, savings);
        }

        return null;
    }

    private static int EstimateRegularFormatSize(List<Dictionary<string, object?>> rows, List<string> keys)
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

    private static int EstimateTabularFormatSize(List<Dictionary<string, object?>> rows, List<string> keys)
    {
        // Estimate: "key[N]{field1,field2}:\n  value1,value2\n  ..."
        // Note: We don't know the key name here, so estimate based on first key
        int size = keys[0].Length + 3; // "key" (estimate)
        size += rows.Count.ToString().Length + 2; // "[N]"
        size += keys.Sum(k => k.Length) + keys.Count - 1; // "{field1,field2}"
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

