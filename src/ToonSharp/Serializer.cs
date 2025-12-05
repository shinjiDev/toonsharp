using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ToonSharp;

/// <summary>
/// Serializer that converts .NET objects into TOON text.
/// </summary>
public class ToonSerializer
{
    private readonly int indent;
    private readonly string mode;
    private readonly int arrayParallelThreshold;
    private readonly int tableParallelThreshold;

    // Cache indent strings to avoid repeated allocations (up to 20 levels)
    private static readonly string[] IndentCache = CreateIndentCache(20);

    private static string[] CreateIndentCache(int maxLevel)
    {
        var cache = new string[maxLevel];
        for (int i = 0; i < maxLevel; i++)
        {
            cache[i] = new string(' ', i);
        }
        return cache;
    }

    public ToonSerializer(int indent = 2, string mode = "auto", int arrayParallelThreshold = 200, int tableParallelThreshold = 75)
    {
        this.indent = indent;
        this.mode = mode ?? "auto";
        this.arrayParallelThreshold = arrayParallelThreshold;
        this.tableParallelThreshold = tableParallelThreshold;
    }

    // Helper method to get indentation efficiently
    private static string GetIndent(int level)
    {
        return level < IndentCache.Length
            ? IndentCache[level]
            : new string(' ', level);
    }

    public string Dumps(object? obj)
    {
        var lines = new List<string>();
        WriteValue(obj, 0, lines);
        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private void WriteValue(object? obj, int level, List<string> lines)
    {
        string indent = GetIndent(level);

        if (obj == null)
        {
            lines.Add(indent + "null");
            return;
        }

        // Normalize complex types (JsonElement, POCO) to Dictionary/List/primitives
        obj = NormalizeObject(obj);
        
        if (obj == null)
        {
            lines.Add(indent + "null");
            return;
        }

        // Use GetType() for faster exact type checks (consistent with IsInline optimization)
        var type = obj.GetType();

        // Dictionary - most common container type, check first
        if (type == typeof(Dictionary<string, object?>))
        {
            var dict = (Dictionary<string, object?>)obj;
            if (dict.Count == 0)
            {
                lines.Add(indent + "{}");
                return;
            }
            WriteObject(dict, level, lines);
            return;
        }

        // String must be checked before IEnumerable (string implements IEnumerable)
        if (type == typeof(string))
        {
            lines.Add(indent + FormatScalar(obj));
            return;
        }

        // List - common case, optimized
        if (type == typeof(List<object?>))
        {
            var list = (List<object?>)obj;
            if (list.Count == 0)
            {
                lines.Add(indent + "[]");
                return;
            }
            // Try tabular format for root-level arrays of uniform dictionaries
            if (TryWriteRootArrayAsTabular(list, level, lines))
            {
                return;
            }
            WriteArray(list, level, lines);
            return;
        }

        // Other IEnumerable (less common, checked last)
        if (obj is System.Collections.IEnumerable enumerable)
        {
            // Normalize all items and collect into list
            var normalizedItems = new List<object?>();
            foreach (var item in enumerable)
            {
                normalizedItems.Add(NormalizeObject(item));
            }
            
            if (normalizedItems.Count == 0)
            {
                lines.Add(indent + "[]");
                return;
            }
            
            // Try tabular format for root-level arrays of uniform dictionaries
            if (TryWriteRootArrayAsTabular(normalizedItems, level, lines))
            {
                return;
            }
            
            WriteArray(normalizedItems, level, lines);
            return;
        }

        // Scalar by default
        lines.Add(indent + FormatScalar(obj));
    }

    private void WriteObject(Dictionary<string, object?> mapping, int level, List<string> lines)
    {
        foreach (var kvp in mapping)
        {
            var keyRepr = Utils.FormatKey(kvp.Key);
            var value = kvp.Value;

            // Optimization: check exact type first (avoids unnecessary casting)
            if (value != null && value.GetType() != typeof(string))
            {
                // Check if it's List<object?> directly (most common case)
                if (value is List<object?> list && list.Count > 0)
                {
                    if (TryWriteAsTabular(keyRepr, list, level, lines))
                        continue;
                }
                // Fallback for other IEnumerable
                else if (value is System.Collections.IEnumerable valueEnumerable)
                {
                    // Use ICollection to avoid enumerating twice
                    if (valueEnumerable is System.Collections.ICollection collection && collection.Count > 0)
                    {
                        if (TryWriteAsTabularFromEnumerable(keyRepr, valueEnumerable, level, lines))
                            continue;
                    }
                }
            }

            // Cache the prefix (used multiple times)
            var prefix = string.Concat(GetIndent(level), keyRepr, ":");

            var inlineContainer = InlineContainerRepr(value);
            if (inlineContainer != null)
            {
                lines.Add(string.Concat(prefix, " ", inlineContainer));
                continue;
            }

            // Optimization: check type only once
            var isContainer = value != null &&
                             (value.GetType() == typeof(Dictionary<string, object?>) ||
                              value.GetType() == typeof(List<object?>) ||
                              (value is System.Collections.IEnumerable && value.GetType() != typeof(string)));

            if (isContainer)
            {
                lines.Add(prefix);
                WriteValue(value, level + indent, lines);
            }
            else if (IsInline(value))
            {
                lines.Add(string.Concat(prefix, " ", FormatScalar(value)));
            }
            else
            {
                lines.Add(prefix);
                WriteValue(value, level + indent, lines);
            }
        }
    }

    // Try to write a root-level array as tabular format
    private bool TryWriteRootArrayAsTabular(List<object?> items, int level, List<string> lines)
    {
        // Early exit: check first element is a dictionary
        if (items[0] is not Dictionary<string, object?> firstDict)
            return false;

        // Check remaining elements (optimized with for)
        for (int i = 1; i < items.Count; i++)
        {
            if (items[i] is not Dictionary<string, object?>)
                return false;
        }

        // All are dictionaries, create list without additional casting
        var dictList = new List<Dictionary<string, object?>>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            dictList.Add((Dictionary<string, object?>)items[i]!);
        }

        var schema = Utils.TabularSchema(dictList);
        if (schema != null)
        {
            bool shouldUseTabular = mode == "compact" ||
                                  mode == "auto" ||
                                  (mode == "readable" && schema.Savings > 10);
            if (shouldUseTabular)
            {
                WriteRootTable(dictList, schema, level, lines);
                return true;
            }
        }

        return false;
    }

    // Write a root-level tabular array (no key name)
    private void WriteRootTable(List<Dictionary<string, object?>> rows, TabularSchema schema, int level, List<string> lines)
    {
        var fields = string.Join(",", schema.Keys);
        var header = string.Concat(GetIndent(level), "[", rows.Count.ToString(), "]{", fields, "}:");
        lines.Add(header);

        var indentStr = GetIndent(level + indent);
        var keysArray = schema.Keys.ToArray();
        var keysCount = keysArray.Length;

        if (rows.Count >= tableParallelThreshold)
        {
            var rowLines = new string[rows.Count];
            Parallel.For(0, rows.Count, i =>
            {
                var row = rows[i];
                var sb = new StringBuilder(indentStr.Length + keysCount * 10);
                sb.Append(indentStr);
                for (int j = 0; j < keysCount; j++)
                {
                    if (j > 0) sb.Append(',');
                    var value = row.GetValueOrDefault(keysArray[j]);
                    sb.Append(FormatScalar(value));
                }
                rowLines[i] = sb.ToString();
            });
            lines.AddRange(rowLines);
        }
        else
        {
            foreach (var row in rows)
            {
                var sb = new StringBuilder(indentStr.Length + keysCount * 10);
                sb.Append(indentStr);
                for (int j = 0; j < keysCount; j++)
                {
                    if (j > 0) sb.Append(',');
                    var value = row.GetValueOrDefault(keysArray[j]);
                    sb.Append(FormatScalar(value));
                }
                lines.Add(sb.ToString());
            }
        }
    }

    // Helper method extracted to avoid code duplication
    private bool TryWriteAsTabular(string keyRepr, List<object?> items, int level, List<string> lines)
    {
        // Early exit: check first element
        if (items[0] is not Dictionary<string, object?> firstDict)
            return false;

        // Check remaining elements (optimized with for)
        for (int i = 1; i < items.Count; i++)
        {
            if (items[i] is not Dictionary<string, object?>)
                return false;
        }

        // All are dictionaries, create list without additional casting
        var dictList = new List<Dictionary<string, object?>>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            dictList.Add((Dictionary<string, object?>)items[i]!);
        }

        var schema = Utils.TabularSchema(dictList);
        if (schema != null)
        {
            bool shouldUseTabular = mode == "compact" ||
                                  mode == "auto" ||
                                  (mode == "readable" && schema.Savings > 10);
            if (shouldUseTabular)
            {
                WriteTableAsKey(keyRepr, dictList, schema, level, lines);
                return true;
            }
        }

        return false;
    }

    private bool TryWriteAsTabularFromEnumerable(string keyRepr, System.Collections.IEnumerable valueEnumerable, int level, List<string> lines)
    {
        var items = valueEnumerable.Cast<object?>().ToList();
        return items.Count > 0 && TryWriteAsTabular(keyRepr, items, level, lines);
    }

    private void WriteArray(List<object?> seq, int level, List<string> lines)
    {
        var prefix = string.Concat(GetIndent(level), "-");

        // Check if all are inline (optimized)
        bool allInline = true;
        for (int i = 0; i < seq.Count; i++)
        {
            if (!IsInline(seq[i]))
            {
                allInline = false;
                break;
            }
        }

        // Parallel only if worth it (large arrays AND all inline)
        if (allInline && seq.Count >= arrayParallelThreshold)
        {
            var itemLines = new string[seq.Count];
            var prefixWithSpace = string.Concat(prefix, " ");

            Parallel.For(0, seq.Count, i =>
            {
                // Avoid repeated concatenation in each iteration
                itemLines[i] = string.Concat(prefixWithSpace, FormatScalar(seq[i]));
            });
            lines.AddRange(itemLines);
        }
        else
        {
            // Sequential optimized: pre-calculate prefix with space
            if (allInline)
            {
                var prefixWithSpace = string.Concat(prefix, " ");
                // Known capacity: optimizes List internally
                var capacity = lines.Capacity;
                if (lines.Count + seq.Count > capacity)
                {
                    lines.Capacity = lines.Count + seq.Count;
                }

                foreach (var item in seq)
                {
                    lines.Add(string.Concat(prefixWithSpace, FormatScalar(item)));
                }
            }
            else
            {
                // Complex items (not inline)
                foreach (var item in seq)
                {
                    if (IsInline(item))
                    {
                        lines.Add(string.Concat(prefix, " ", FormatScalar(item)));
                    }
                    else
                    {
                        lines.Add(prefix);
                        WriteValue(item, level + indent, lines);
                    }
                }
            }
        }
    }

    private void WriteTableAsKey(string key, List<Dictionary<string, object?>> rows, TabularSchema schema, int level, List<string> lines)
    {
        // Optimization: build header efficiently
        var fields = string.Join(",", schema.Keys);
        var header = string.Concat(GetIndent(level), key, "[", rows.Count.ToString(), "]{", fields, "}:");
        lines.Add(header);

        var indentStr = GetIndent(level + indent);

        if (rows.Count >= tableParallelThreshold)
        {
            var rowLines = new string[rows.Count];
            var keysArray = schema.Keys.ToArray(); // Avoid enumerating multiple times
            var keysCount = keysArray.Length;

            Parallel.For(0, rows.Count, i =>
            {
                var row = rows[i];
                // StringBuilder is more efficient for multiple concatenations
                var sb = new StringBuilder(indentStr.Length + keysCount * 10);
                sb.Append(indentStr);
                for (int j = 0; j < keysCount; j++)
                {
                    if (j > 0) sb.Append(',');
                    var value = row.GetValueOrDefault(keysArray[j]);
                    sb.Append(FormatScalar(value));
                }
                rowLines[i] = sb.ToString();
            });
            lines.AddRange(rowLines);
        }
        else
        {
            // Sequential optimized with StringBuilder
            var keysArray = schema.Keys.ToArray();
            var keysCount = keysArray.Length;

            // Pre-allocate capacity
            if (lines.Capacity < lines.Count + rows.Count)
            {
                lines.Capacity = lines.Count + rows.Count;
            }

            // Reuse StringBuilder to reduce allocations
            var sb = new StringBuilder(indentStr.Length + keysCount * 10);
            foreach (var row in rows)
            {
                sb.Clear();
                sb.Append(indentStr);
                for (int j = 0; j < keysCount; j++)
                {
                    if (j > 0) sb.Append(',');
                    var value = row.GetValueOrDefault(keysArray[j]);
                    sb.Append(FormatScalar(value));
                }
                lines.Add(sb.ToString());
            }
        }
    }

    private bool IsInline(object? value)
    {
        if (value == null)
        {
            return true;
        }

        // GetType() is faster than 'is' for exact type checks (benchmarked: 2.78x faster)
        // Safe in this context since parser always creates exact Dictionary/List types
        var type = value.GetType();
        
        // Comparison by type reference (very fast)
        if (type == typeof(Dictionary<string, object?>))
        {
            return false;
        }
        
        if (type == typeof(List<object?>))
        {
            return false;
        }

        // For strings, IndexOf is faster than Contains for single character checks
        if (type == typeof(string))
        {
            return ((string)value).IndexOf('\n') == -1;
        }

        return true;
    }

    private string? InlineContainerRepr(object? value)
    {
        if (value == null) return null;

        var type = value.GetType();

        // Type reference comparison (extremely fast)
        if (type == typeof(Dictionary<string, object?>))
        {
            return ((Dictionary<string, object?>)value).Count == 0 ? "{}" : null;
        }

        if (type == typeof(List<object?>))
        {
            return ((List<object?>)value).Count == 0 ? "[]" : null;
        }

        return null;
    }

    private string FormatScalar(object? value)
    {
        return Utils.FormatScalar(value);
    }

    /// <summary>
    /// Normalize an object to TOON-compatible types (Dictionary, List, primitives).
    /// Handles JsonElement, POCO objects, and other complex types.
    /// </summary>
    private object? NormalizeObject(object? obj)
    {
        if (obj == null) return null;

        var type = obj.GetType();

        // Already normalized types - return as-is
        if (type == typeof(Dictionary<string, object?>) ||
            type == typeof(List<object?>) ||
            type == typeof(string) ||
            type.IsPrimitive ||
            type == typeof(decimal))
        {
            return obj;
        }

        // Handle JsonElement (from System.Text.Json deserialization)
        if (obj is JsonElement jsonElement)
        {
            return NormalizeJsonElement(jsonElement);
        }

        // Handle IDictionary<string, object?> variants
        if (obj is IDictionary<string, object?> dictInterface)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in dictInterface)
            {
                result[kvp.Key] = NormalizeObject(kvp.Value);
            }
            return result;
        }

        // Handle generic lists/arrays
        if (obj is System.Collections.IEnumerable enumerable && type != typeof(string))
        {
            var result = new List<object?>();
            foreach (var item in enumerable)
            {
                result.Add(NormalizeObject(item));
            }
            return result;
        }

        // Handle POCO objects via reflection
        // Support classes with or without namespace (but not System.* types)
        if (type.IsClass && !type.IsAbstract)
        {
            // Skip System types (but allow user types without namespace)
            if (type.Namespace == null || !type.Namespace.StartsWith("System"))
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (properties.Length > 0 || fields.Length > 0)
                {
                    return NormalizePocoObject(obj, type);
                }
            }
        }

        // For other complex types (anonymous types, structs, etc.), try reflection
        if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Length > 0 || fields.Length > 0)
            {
                return NormalizePocoObject(obj, type);
            }
        }

        // Return primitive-like types as-is
        return obj;
    }

    /// <summary>
    /// Convert a JsonElement to normalized .NET types.
    /// </summary>
    private object? NormalizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = NormalizeJsonElement(prop.Value);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(NormalizeJsonElement(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                // Try to preserve integer types when possible
                if (element.TryGetInt64(out var longValue))
                {
                    // Use int if it fits, otherwise long
                    if (longValue >= int.MinValue && longValue <= int.MaxValue)
                        return (int)longValue;
                    return longValue;
                }
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return null;
        }
    }

    /// <summary>
    /// Convert a POCO object to a Dictionary using reflection.
    /// </summary>
    private Dictionary<string, object?> NormalizePocoObject(object obj, Type type)
    {
        var result = new Dictionary<string, object?>();
        
        // Get all public instance properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            // Skip indexed properties
            if (prop.GetIndexParameters().Length > 0) continue;
            
            // Skip properties that can't be read
            if (!prop.CanRead) continue;

            try
            {
                var value = prop.GetValue(obj);
                result[prop.Name] = NormalizeObject(value);
            }
            catch
            {
                // Skip properties that throw exceptions
            }
        }

        // Also check public fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            try
            {
                var value = field.GetValue(obj);
                result[field.Name] = NormalizeObject(value);
            }
            catch
            {
                // Skip fields that throw exceptions
            }
        }

        return result;
    }
}

