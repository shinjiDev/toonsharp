using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
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
    public string Delimiter { get; set; }

    public TabularSchema(List<string> keys, int savings, string delimiter = ",")
    {
        Keys = keys;
        Savings = savings;
        Delimiter = delimiter;
    }
}

/// <summary>
/// Helper utilities shared between the parser and serializer.
/// </summary>
public static class Utils
{
    private static readonly JsonSerializerOptions QuotedJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

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
    public static bool IsDotSeparatedFoldableKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.IndexOf('.') < 0)
        {
            return false;
        }

        foreach (var segment in key.Split('.'))
        {
            if (!KeyFolding.IsFoldableSegment(segment))
            {
                return false;
            }
        }

        return true;
    }

    public static string FormatKey(string key)
    {
        if (IsDotSeparatedFoldableKey(key))
        {
            return key;
        }

        if (IsSafeIdentifier(key) && !key.Contains('-'))
        {
            return key;
        }

        return JsonSerializer.Serialize(key, QuotedJsonOptions);
    }

    /// <summary>
    /// Format a scalar value for TOON output.
    /// </summary>
    public static string FormatScalar(object? value, string? activeTableDelimiter = null)
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
            if (StringNeedsQuotes(str, activeTableDelimiter))
            {
                return JsonSerializer.Serialize(str, QuotedJsonOptions);
            }
            return str;
        }

        if (value is bool)
        {
            return (bool)value ? "true" : "false";
        }

        if (value is double d)
        {
            var sb = new StringBuilder(32);
            AppendNumber(sb, d);
            return sb.ToString();
        }

        if (value is float f)
        {
            var sb = new StringBuilder(32);
            AppendNumber(sb, f);
            return sb.ToString();
        }

        if (value is decimal dec)
        {
            var sb = new StringBuilder(32);
            AppendNumber(sb, (double)dec);
            return sb.ToString();
        }

        if (value is long l)
        {
            return l.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value is int i)
        {
            return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetRawText();
        }

        return JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Append a scalar to <paramref name="sb"/> without an intermediate string when possible.
    /// </summary>
    public static void AppendScalar(StringBuilder sb, object? value, string? activeTableDelimiter = null)
    {
        if (value == null)
        {
            sb.Append("null");
            return;
        }

        if (value is bool b)
        {
            sb.Append(b ? "true" : "false");
            return;
        }

        if (value is string str)
        {
            if (StringNeedsQuotes(str, activeTableDelimiter))
            {
                AppendQuotedString(sb, str);
            }
            else
            {
                sb.Append(str);
            }

            return;
        }

        if (value is double d)
        {
            AppendNumber(sb, d);
            return;
        }

        if (value is float f)
        {
            AppendNumber(sb, f);
            return;
        }

        if (value is decimal dec)
        {
            AppendNumber(sb, (double)dec);
            return;
        }

        if (value is long l)
        {
            sb.Append(l.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value is int i)
        {
            sb.Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            sb.Append(jsonElement.GetRawText());
            return;
        }

        sb.Append(JsonSerializer.Serialize(value));
    }

    private static void AppendNumber(StringBuilder sb, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            sb.Append(JsonSerializer.Serialize(value));
            return;
        }

        if (value == 0.0)
        {
            sb.Append('0');
            return;
        }

        var text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (text.Contains('E') || text.Contains('e'))
        {
            text = value.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture);
            text = TrimInsignificantZeros(text);
        }

        sb.Append(text);
    }

    private static string TrimInsignificantZeros(string text)
    {
        int dot = text.IndexOf('.');
        if (dot < 0)
        {
            return text;
        }

        int end = text.Length - 1;
        while (end > dot && text[end] == '0')
        {
            end--;
        }

        if (end == dot)
        {
            end--;
        }

        return text.Substring(0, end + 1);
    }

    /// <summary>
    /// Check if a string needs quotes in TOON format.
    /// </summary>
    public static bool StringNeedsQuotes(string value, string? activeTableDelimiter = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (value.Trim().Length < value.Length)
        {
            return true;
        }

        if (LooksLikeAmbiguousStringLiteral(value))
        {
            return true;
        }

        if (IsSafeIdentifier(value))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(activeTableDelimiter) && activeTableDelimiter != "," &&
            value.Contains(activeTableDelimiter, StringComparison.Ordinal))
        {
            return true;
        }

        return value.Any(c =>
            c == '"' ||
            c == '\n' ||
            c == '\r' ||
            c == '\t' ||
            c == ':' ||
            c == '-' ||
            c == '[' ||
            c == ']' ||
            c == '{' ||
            c == '}' ||
            (c == ',' && (activeTableDelimiter == null || activeTableDelimiter == ",")));
    }

    /// <summary>
    /// JSON strings that look like booleans, null, or numbers must be quoted in TOON output.
    /// </summary>
    public static bool LooksLikeAmbiguousStringLiteral(string value)
    {
        if (value is "true" or "false" or "null" || NumberRegex.IsMatch(value))
        {
            return true;
        }

        // Leading zeros are strings in JSON, not TOON numbers (e.g. "05")
        if (value.Length > 1 && value[0] == '0' && char.IsDigit(value[1]))
        {
            return true;
        }

        if (value.Length > 2 && value[0] == '-' && value[1] == '0' && char.IsDigit(value[2]))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Format a tabular cell using the active row delimiter (§11).
    /// </summary>
    public static string FormatTableCell(object? value, string activeDelimiter)
    {
        var sb = new StringBuilder(16);
        AppendTableCell(sb, value, activeDelimiter);
        return sb.ToString();
    }

    public static void AppendTableCell(StringBuilder sb, object? value, string activeDelimiter)
    {
        AppendScalar(sb, value, activeDelimiter);
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
        if (token.IsEmpty)
        {
            return null;
        }

        // Leading zeros are strings, not numbers (§4)
        if (token.Length > 1 && token[0] == '0' && char.IsDigit(token[1]))
        {
            return null;
        }

        if (token.Length > 2 && token[0] == '-' && token[1] == '0' && char.IsDigit(token[2]))
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
                return NormalizeNumericValue(d);
            }
        }
        else
        {
            // For integers, try parse directly (faster)
            if (long.TryParse(token, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long l))
            {
                return NormalizeIntegerWidth(l);
            }

            if (!NumberRegex.IsMatch(token.ToString()))
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Prefer integers when a float token is mathematically integral (JSON decode parity).
    /// </summary>
    public static object NormalizeNumericValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return value;
        }

        if (value == Math.Truncate(value) && value >= long.MinValue && value <= long.MaxValue)
        {
            return NormalizeIntegerWidth((long)value);
        }

        return value;
    }

    public static object NormalizeIntegerWidth(long value) => value;

    /// <summary>
    /// Detect tabular schema for a sequence of uniform objects.
    /// Optimized sequential version - avoids parallel overhead for better performance on medium datasets.
    /// </summary>
    public static TabularSchema? TabularSchema(
        IEnumerable<Dictionary<string, object?>> rows,
        int minKeyCount = 2,
        string? delimiterOverride = null)
    {
        var rowList = rows as List<Dictionary<string, object?>> ?? rows.ToList();
        
        if (rowList.Count == 0)
        {
            return null;
        }

        var firstDict = rowList[0];
        if (firstDict.Count < minKeyCount)
        {
            return null;
        }

        // Use ToArray() for keys - more efficient than ToList() for read-only access
        var firstKeys = firstDict.Keys.ToArray();
        var keyCount = firstKeys.Length;
        bool needsNonComma = false;
        bool needsNonPipe = false;

        for (int i = 0; i < rowList.Count; i++)
        {
            var row = rowList[i];

            if (row.Count != keyCount)
            {
                return null;
            }

            if (i > 0)
            {
                for (int j = 0; j < keyCount; j++)
                {
                    if (!row.ContainsKey(firstKeys[j]))
                    {
                        return null;
                    }
                }
            }

            for (int j = 0; j < keyCount; j++)
            {
                var cell = row[firstKeys[j]];
                if (!IsTabularPrimitiveValue(cell))
                {
                    return null;
                }

                if (cell is string s)
                {
                    if (!needsNonComma && s.Contains(','))
                    {
                        needsNonComma = true;
                    }

                    if (!needsNonPipe && s.Contains('|'))
                    {
                        needsNonPipe = true;
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
            var keys = firstKeys.ToList();
            var delimiter = delimiterOverride ?? ChooseTableDelimiter(keys, rowList, needsNonComma, needsNonPipe, stringFlagsComputed: true);
            return new TabularSchema(keys, savings, delimiter);
        }

        return null;
    }

    private static bool IsTabularPrimitiveValue(object? value)
    {
        if (value == null)
        {
            return true;
        }

        var type = value.GetType();
        return type == typeof(string) || type.IsPrimitive || type == typeof(decimal);
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

    private static readonly string[] TableDelimiterCandidates = { ",", "|", "\t" };

    /// <summary>
    /// Pick the active tabular delimiter (§11): prefer comma, then pipe, then tab when
    /// field names or unquoted cell values would collide with the delimiter.
    /// </summary>
    public static string ChooseTableDelimiter(
        IReadOnlyList<string> keys,
        IReadOnlyList<Dictionary<string, object?>> rows,
        bool needsNonComma = false,
        bool needsNonPipe = false,
        bool stringFlagsComputed = false)
    {
        if (!stringFlagsComputed)
        {
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int k = 0; k < keys.Count; k++)
                {
                    if (!row.TryGetValue(keys[k], out var value) || value is not string s)
                    {
                        continue;
                    }

                    if (!needsNonComma && s.Contains(','))
                    {
                        needsNonComma = true;
                    }

                    if (!needsNonPipe && s.Contains('|'))
                    {
                        needsNonPipe = true;
                    }
                }
            }

            stringFlagsComputed = true;
        }

        // Prefer comma; cell values that contain the delimiter are quoted (§11).
        if (IsTableDelimiterViable(",", keys, rows, skipStringScan: true))
        {
            return ",";
        }

        if (IsTableDelimiterViable("|", keys, rows, skipStringScan: true))
        {
            return "|";
        }

        if (IsTableDelimiterViable("\t", keys, rows, skipStringScan: true))
        {
            return "\t";
        }

        return ",";
    }

    private static bool IsTableDelimiterViable(
        string delimiter,
        IReadOnlyList<string> keys,
        IReadOnlyList<Dictionary<string, object?>> rows,
        bool skipStringScan = false)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].Contains(delimiter, StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (skipStringScan && delimiter == ",")
        {
            return true;
        }

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (int k = 0; k < keys.Count; k++)
            {
                row.TryGetValue(keys[k], out var value);
                if (value is string s)
                {
                    if (s.Contains(delimiter, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    continue;
                }

                var scalar = FormatScalar(value);
                if (ScalarContainsUnquotedDelimiter(scalar, delimiter))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static void AppendQuotedString(StringBuilder sb, string value)
    {
        sb.Append(JsonSerializer.Serialize(value, QuotedJsonOptions));
    }

    public static int EstimateScalarEncodedLength(object? value, string? activeTableDelimiter = null)
    {
        if (value == null)
        {
            return 4;
        }

        if (value is bool)
        {
            return value.Equals(true) ? 4 : 5;
        }

        if (value is string str)
        {
            if (!StringNeedsQuotes(str, activeTableDelimiter))
            {
                return str.Length;
            }

            int extra = 2;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] is '"' or '\\')
                {
                    extra++;
                }
            }

            return str.Length + extra;
        }

        return FormatScalar(value).Length;
    }

    private static bool ScalarContainsUnquotedDelimiter(string scalar, string delimiter)
    {
        if (scalar.Length == 0)
        {
            return false;
        }

        if (scalar[0] == '"')
        {
            return ContainsDelimiterOutsideJsonString(scalar, delimiter);
        }

        return scalar.Contains(delimiter, StringComparison.Ordinal);
    }

    private static bool ContainsDelimiterOutsideJsonString(string scalar, string delimiter)
    {
        bool inQuotes = false;
        bool escape = false;

        for (int i = 0; i < scalar.Length; i++)
        {
            char ch = scalar[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && i + delimiter.Length <= scalar.Length &&
                scalar.AsSpan(i, delimiter.Length).SequenceEqual(delimiter.AsSpan()))
            {
                return true;
            }
        }

        return false;
    }
}

