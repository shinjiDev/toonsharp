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
        var writer = new ToonWriter(4096);
        WriteValue(obj, 0, writer);
        return writer.ToString();
    }

    private void WriteValue(object? obj, int level, ToonWriter writer)
    {
        string indent = GetIndent(level);

        if (obj == null)
        {
            writer.AddLine(indent + "null");
            return;
        }

        // Normalize complex types (JsonElement, POCO) to Dictionary/List/primitives
        obj = NormalizeObject(obj);
        
        if (obj == null)
        {
            writer.AddLine(indent + "null");
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
                writer.AddLine(indent + "{}");
                return;
            }
            WriteObject(dict, level, writer);
            return;
        }

        // String must be checked before IEnumerable (string implements IEnumerable)
        if (type == typeof(string))
        {
            writer.AddLine(indent + FormatScalar(obj));
            return;
        }

        // List - common case, optimized
        if (type == typeof(List<object?>))
        {
            var list = (List<object?>)obj;
            if (list.Count == 0)
            {
                writer.AddLine(indent + "[]");
                return;
            }
            if (level == 0 && TryWriteRootArrayAsTabular(list, level, writer))
            {
                return;
            }
            WriteArray(list, level, writer);
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
                writer.AddLine(indent + "[]");
                return;
            }
            
            if (level == 0 && TryWriteRootArrayAsTabular(normalizedItems, level, writer))
            {
                return;
            }
            
            WriteArray(normalizedItems, level, writer);
            return;
        }

        // Scalar by default
        writer.AddLine(indent + FormatScalar(obj));
    }

    private void WriteObject(Dictionary<string, object?> mapping, int level, ToonWriter writer)
    {
        foreach (var kvp in mapping)
        {
            var keyRepr = Utils.FormatKey(kvp.Key);
            var value = kvp.Value;

            if (TryNormalizeList(value, out var listValue))
            {
                if (listValue.Count > 0 && TryWriteAsTabular(keyRepr, listValue, level, writer))
                {
                    continue;
                }

                if (TryWriteInlineArrayField(writer, level, keyRepr, listValue))
                {
                    continue;
                }

                writer.AddLine(string.Concat(GetIndent(level), keyRepr, "[", listValue.Count.ToString(), "]:"));
                WriteArray(listValue, level + indent, writer);
                continue;
            }

            // Cache the prefix (used multiple times)
            var prefix = string.Concat(GetIndent(level), keyRepr, ":");

            var inlineContainer = InlineContainerRepr(value);
            if (inlineContainer != null)
            {
                writer.AddLine(string.Concat(prefix, " ", inlineContainer));
                continue;
            }

            // Optimization: check type only once
            var isContainer = value != null &&
                             (value.GetType() == typeof(Dictionary<string, object?>) ||
                              (value is System.Collections.IEnumerable && value.GetType() != typeof(string)));

            if (isContainer)
            {
                writer.AddLine(prefix);
                WriteValue(value, level + indent, writer);
            }
            else if (IsInline(value))
            {
                writer.AddLine(string.Concat(prefix, " ", FormatScalar(value)));
            }
            else
            {
                writer.AddLine(prefix);
                WriteValue(value, level + indent, writer);
            }
        }
    }

    // Try to write a root-level array as tabular format
    private bool TryWriteRootArrayAsTabular(List<object?> items, int level, ToonWriter writer)
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
                WriteRootTable(dictList, schema, level, writer);
                return true;
            }
        }

        return false;
    }

    // Write a root-level tabular array (no key name)
    private void WriteRootTable(List<Dictionary<string, object?>> rows, TabularSchema schema, int level, ToonWriter writer)
    {
        if (schema.Delimiter == ",")
        {
            var fields = string.Join(",", schema.Keys);
            writer.AddLine(string.Concat(GetIndent(level), "[", rows.Count.ToString(), "]{", fields, "}:"));
            WriteCommaTableRows(rows, schema.Keys, level + indent, writer);
            return;
        }

        writer.AddLine(string.Concat(
            GetIndent(level),
            FormatTableHeaderBody(rows.Count, schema.Keys, schema.Delimiter)));
        WriteTableRows(rows, schema.Keys, schema.Delimiter, level + indent, writer);
    }

    private void WriteCommaTableRows(
        List<Dictionary<string, object?>> rows,
        IReadOnlyList<string> keys,
        int rowLevel,
        ToonWriter writer)
    {
        var indentStr = GetIndent(rowLevel);
        var keysArray = keys as string[] ?? keys.ToArray();
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
                    if (j > 0)
                    {
                        sb.Append(',');
                    }

                    row.TryGetValue(keysArray[j], out var value);
                    Utils.AppendScalar(sb, value);
                }

                rowLines[i] = sb.ToString();
            });
            writer.AddLines(rowLines);
        }
        else
        {
            writer.EnsureCapacity(rows.Count * (indentStr.Length + keysCount * 8));
            foreach (var row in rows)
            {
                writer.NewLine();
                writer.Append(indentStr);
                AppendTableRowCells(writer.Buffer, row, keysArray, keysCount, ",");
            }
        }
    }

    // Helper method extracted to avoid code duplication
    private bool TryWriteAsTabular(string keyRepr, List<object?> items, int level, ToonWriter writer)
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
                WriteTableAsKey(keyRepr, dictList, schema, level, writer);
                return true;
            }
        }

        return false;
    }

    private bool TryWriteAsTabularFromEnumerable(string keyRepr, System.Collections.IEnumerable valueEnumerable, int level, ToonWriter writer)
    {
        var items = valueEnumerable.Cast<object?>().ToList();
        return items.Count > 0 && TryWriteAsTabular(keyRepr, items, level, writer);
    }

    private void WriteArray(List<object?> seq, int level, ToonWriter writer)
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
            writer.AddLines(itemLines);
        }
        else
        {
            // Sequential optimized: pre-calculate prefix with space
            if (allInline)
            {
                var prefixWithSpace = string.Concat(prefix, " ");
                writer.EnsureCapacity(seq.Count * (prefixWithSpace.Length + 12));

                foreach (var item in seq)
                {
                    writer.NewLine();
                    writer.Append(prefixWithSpace);
                    Utils.AppendScalar(writer.Buffer, item);
                }
            }
            else
            {
                // Complex items (not inline)
                foreach (var item in seq)
                {
                    if (IsInline(item))
                    {
                        writer.AddLine(string.Concat(prefix, " ", FormatScalar(item)));
                    }
                    else if (item is Dictionary<string, object?> dict)
                    {
                        WriteListItemObject(dict, level, prefix, writer);
                    }
                    else
                    {
                        writer.AddLine(prefix);
                        WriteValue(item, level + indent, writer);
                    }
                }
            }
        }
    }

    /// <summary>
    /// TOON v3 §10: encode an object as a list item (- line).
    /// </summary>
    private void WriteListItemObject(Dictionary<string, object?> dict, int level, string hyphenPrefix, ToonWriter writer)
    {
        if (dict.Count == 0)
        {
            writer.AddLine(hyphenPrefix);
            return;
        }

        using var enumerator = dict.GetEnumerator();
        enumerator.MoveNext();
        var first = enumerator.Current;
        var firstKeyRepr = Utils.FormatKey(first.Key);

        if (TryGetTabularListForListItem(first.Value, out var tabularRows, out var tabularSchema))
        {
            writer.AddLine(string.Concat(
                hyphenPrefix,
                " ",
                firstKeyRepr,
                FormatTableHeaderBody(tabularRows.Count, tabularSchema.Keys, tabularSchema.Delimiter)));
            WriteTableRows(tabularRows, tabularSchema.Keys, tabularSchema.Delimiter, level + indent * 2, writer);

            var siblingLevel = level + indent;
            while (enumerator.MoveNext())
            {
                WriteObjectField(enumerator.Current.Key, enumerator.Current.Value, siblingLevel, writer);
            }
            return;
        }

        WriteListItemFirstFieldOnHyphen(firstKeyRepr, first.Value, hyphenPrefix, level, writer);
        var fieldLevel = level + indent;
        while (enumerator.MoveNext())
        {
            WriteObjectField(enumerator.Current.Key, enumerator.Current.Value, fieldLevel, writer);
        }
    }

    private void WriteListItemFirstFieldOnHyphen(string keyRepr, object? value, string hyphenPrefix, int level, ToonWriter writer)
    {
        var inlineContainer = InlineContainerRepr(value);
        if (inlineContainer != null)
        {
            writer.AddLine(string.Concat(hyphenPrefix, " ", keyRepr, ": ", inlineContainer));
            return;
        }

        if (value is List<object?> list && list.Count > 0)
        {
            if (TryWriteInlineArrayOnHyphen(writer, keyRepr, list, hyphenPrefix))
            {
                return;
            }
        }

        if (IsInline(value))
        {
            writer.AddLine(string.Concat(hyphenPrefix, " ", keyRepr, ": ", FormatScalar(value)));
            return;
        }

        writer.AddLine(string.Concat(hyphenPrefix, " ", keyRepr, ":"));
        WriteValue(value, level + indent * 2, writer);
    }

    private bool TryWriteInlineArrayField(ToonWriter writer, int level, string keyRepr, List<object?> list)
    {
        if (list.Count == 0)
        {
            writer.AddLine(string.Concat(GetIndent(level), keyRepr, "[0]:"));
            return true;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (!IsInline(list[i]))
            {
                return false;
            }
        }

        int capacity = GetIndent(level).Length + keyRepr.Length + 16;
        for (int i = 0; i < list.Count; i++)
        {
            capacity += Utils.EstimateScalarEncodedLength(list[i]) + 1;
        }

        writer.EnsureCapacity(capacity);
        writer.NewLine();
        writer.Append(GetIndent(level));
        writer.Append(keyRepr);
        writer.Append('[');
        writer.Append(list.Count.ToString());
        writer.Append("]: ");

        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                writer.Append(',');
            }

            Utils.AppendScalar(writer.Buffer, list[i]);
        }

        return true;
    }

    private bool TryWriteInlineArrayOnHyphen(
        ToonWriter writer,
        string keyRepr,
        List<object?> list,
        string hyphenPrefix)
    {
        if (list.Count == 0)
        {
            writer.AddLine(string.Concat(hyphenPrefix, " ", keyRepr, "[0]:"));
            return true;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (!IsInline(list[i]))
            {
                return false;
            }
        }

        writer.EnsureCapacity(hyphenPrefix.Length + keyRepr.Length + list.Count * 12);
        writer.NewLine();
        writer.Append(hyphenPrefix);
        writer.Append(' ');
        writer.Append(keyRepr);
        writer.Append('[');
        writer.Append(list.Count.ToString());
        writer.Append("]: ");

        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                writer.Append(',');
            }

            Utils.AppendScalar(writer.Buffer, list[i]);
        }

        return true;
    }

    private bool TryGetTabularListForListItem(object? value, out List<Dictionary<string, object?>> rows, out TabularSchema schema)
    {
        rows = null!;
        schema = null!;

        if (value is not List<object?> list || list.Count == 0 || list[0] is not Dictionary<string, object?> firstDict)
        {
            return false;
        }

        for (int i = 1; i < list.Count; i++)
        {
            if (list[i] is not Dictionary<string, object?>)
            {
                return false;
            }
        }

        rows = new List<Dictionary<string, object?>>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            rows.Add((Dictionary<string, object?>)list[i]!);
        }

        var detected = Utils.TabularSchema(rows, minKeyCount: 1);
        if (detected == null)
        {
            return false;
        }

        if (mode != "compact" && mode != "auto" && !(mode == "readable" && detected.Savings > 10))
        {
            return false;
        }

        schema = detected;
        return true;
    }

    private void WriteObjectField(string key, object? value, int level, ToonWriter writer)
    {
        var keyRepr = Utils.FormatKey(key);

        if (TryNormalizeList(value, out var listValue))
        {
            if (listValue.Count > 0 && TryWriteAsTabular(keyRepr, listValue, level, writer))
            {
                return;
            }

            if (TryWriteInlineArrayField(writer, level, keyRepr, listValue))
            {
                return;
            }

            if (listValue.Count > 0)
            {
                writer.AddLine(string.Concat(GetIndent(level), keyRepr, "[", listValue.Count.ToString(), "]:"));
                WriteArray(listValue, level + indent, writer);
                return;
            }
        }

        if (value != null && value.GetType() != typeof(string))
        {
            if (value is System.Collections.IEnumerable valueEnumerable &&
                valueEnumerable is System.Collections.ICollection collection &&
                collection.Count > 0 &&
                TryWriteAsTabularFromEnumerable(keyRepr, valueEnumerable, level, writer))
            {
                return;
            }
        }

        var prefix = string.Concat(GetIndent(level), keyRepr, ":");
        var inlineContainer = InlineContainerRepr(value);
        if (inlineContainer != null)
        {
            writer.AddLine(string.Concat(prefix, " ", inlineContainer));
            return;
        }

        var isContainer = value != null &&
                         (value.GetType() == typeof(Dictionary<string, object?>) ||
                          value.GetType() == typeof(List<object?>) ||
                          (value is System.Collections.IEnumerable && value.GetType() != typeof(string)));

        if (isContainer)
        {
            writer.AddLine(prefix);
            WriteValue(value, level + indent, writer);
        }
        else if (IsInline(value))
        {
            writer.AddLine(string.Concat(prefix, " ", FormatScalar(value)));
        }
        else
        {
            writer.AddLine(prefix);
            WriteValue(value, level + indent, writer);
        }
    }

    private void WriteTableAsKey(string key, List<Dictionary<string, object?>> rows, TabularSchema schema, int level, ToonWriter writer)
    {
        if (schema.Delimiter == ",")
        {
            WriteCommaTableAsKey(key, rows, schema, level, writer);
            return;
        }

        writer.AddLine(string.Concat(
            GetIndent(level),
            key,
            FormatTableHeaderBody(rows.Count, schema.Keys, schema.Delimiter)));
        WriteTableRows(rows, schema.Keys, schema.Delimiter, level + indent, writer);
    }

    private void WriteCommaTableAsKey(string key, List<Dictionary<string, object?>> rows, TabularSchema schema, int level, ToonWriter writer)
    {
        var fields = string.Join(",", schema.Keys);
        var header = string.Concat(GetIndent(level), key, "[", rows.Count.ToString(), "]{", fields, "}:");
        writer.AddLine(header);
        WriteCommaTableRows(rows, schema.Keys, level + indent, writer);
    }

    private static string FormatTableHeaderBody(int rowCount, IReadOnlyList<string> keys, string delimiter)
    {
        var bracketSuffix = delimiter == "," ? "" : delimiter;
        var sb = new StringBuilder(32 + keys.Count * 8);
        sb.Append('[');
        sb.Append(rowCount);
        sb.Append(bracketSuffix);
        sb.Append("]{");
        for (int i = 0; i < keys.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(delimiter);
            }

            sb.Append(keys[i]);
        }

        sb.Append("}:");
        return sb.ToString();
    }

    private void WriteTableRows(
        List<Dictionary<string, object?>> rows,
        IReadOnlyList<string> keys,
        string delimiter,
        int rowLevel,
        ToonWriter writer)
    {
        var indentStr = GetIndent(rowLevel);
        var keysArray = keys as string[] ?? keys.ToArray();
        var keysCount = keysArray.Length;

        if (rows.Count >= tableParallelThreshold)
        {
            var rowLines = new string[rows.Count];

            Parallel.For(0, rows.Count, i =>
            {
                rowLines[i] = FormatTableRow(indentStr, rows[i], keysArray, keysCount, delimiter);
            });
            writer.AddLines(rowLines);
        }
        else
        {
            writer.EnsureCapacity(rows.Count * (indentStr.Length + keysCount * 12));
            foreach (var row in rows)
            {
                writer.NewLine();
                writer.Append(indentStr);
                AppendTableRowCells(writer.Buffer, row, keysArray, keysCount, delimiter);
            }
        }
    }

    private string FormatTableRow(string indentStr, Dictionary<string, object?> row, string[] keysArray, int keysCount, string delimiter)
    {
        var sb = new StringBuilder(indentStr.Length + keysCount * 12);
        sb.Append(indentStr);
        AppendTableRowCells(sb, row, keysArray, keysCount, delimiter);
        return sb.ToString();
    }

    private static void AppendTableRowCells(
        StringBuilder sb,
        Dictionary<string, object?> row,
        string[] keysArray,
        int keysCount,
        string delimiter)
    {
        for (int j = 0; j < keysCount; j++)
        {
            if (j > 0)
            {
                sb.Append(delimiter);
            }

            row.TryGetValue(keysArray[j], out var value);
            Utils.AppendTableCell(sb, value, delimiter);
        }
    }

    private static bool TryNormalizeList(object? value, out List<object?> list)
    {
        if (value is List<object?> exactList)
        {
            list = exactList;
            return true;
        }

        if (value is System.Collections.IList rawList && value is not string)
        {
            list = new List<object?>(rawList.Count);
            foreach (var item in rawList)
            {
                list.Add(item);
            }
            return true;
        }

        list = null!;
        return false;
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

