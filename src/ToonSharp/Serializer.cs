using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToonSharp;

/// <summary>
/// Serializer that converts .NET objects into TOON text.
/// </summary>
public class ToonSerializer
{
    private readonly int indent;
    private readonly string mode;

    public ToonSerializer(int indent = 2, string mode = "auto")
    {
        this.indent = indent;
        this.mode = mode ?? "auto";
    }

    public string Dumps(object? obj)
    {
        var lines = new List<string>();
        WriteValue(obj, 0, lines);
        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private void WriteValue(object? obj, int level, List<string> lines)
    {
        if (obj == null)
        {
            lines.Add(new string(' ', level) + "null");
            return;
        }

        if (obj is Dictionary<string, object?> dict)
        {
            if (dict.Count == 0)
            {
                lines.Add(new string(' ', level) + "{}");
                return;
            }
            WriteObject(dict, level, lines);
        }
        else if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
        {
            var items = enumerable.Cast<object?>().ToList();
            if (items.Count == 0)
            {
                lines.Add(new string(' ', level) + "[]");
                return;
            }
            WriteArray(items, level, lines);
        }
        else
        {
            lines.Add(new string(' ', level) + FormatScalar(obj));
        }
    }

    private void WriteObject(Dictionary<string, object?> mapping, int level, List<string> lines)
    {
        foreach (var kvp in mapping)
        {
            var keyRepr = Utils.FormatKey(kvp.Key);
            var value = kvp.Value;

            // Check if value is a tabular array (any IEnumerable of dictionaries)
            if (value is System.Collections.IEnumerable valueEnumerable && !(value is string))
            {
                var items = valueEnumerable.Cast<object?>().ToList();
                if (items.Count > 0 && items.All(item => item is Dictionary<string, object?>))
                {
                    var dictList = items.Cast<Dictionary<string, object?>>().ToList();
                    var schema = Utils.TabularSchema(dictList);
                    // In compact mode, always use tabular if schema is detected
                    // In auto mode, use schema if it exists (savings > 0)
                    // In readable mode, only use if savings > 10
                    if (schema != null)
                    {
                        bool shouldUseTabular = mode == "compact" || 
                                              mode == "auto" || 
                                              (mode == "readable" && schema.Savings > 10);
                        if (shouldUseTabular)
                        {
                            WriteTableAsKey(keyRepr, dictList, schema, level, lines);
                            continue;
                        }
                    }
                }
            }

            var prefix = new string(' ', level) + $"{keyRepr}:";
            var inlineContainer = InlineContainerRepr(value);
            if (inlineContainer != null)
            {
                lines.Add($"{prefix} {inlineContainer}");
                continue;
            }

            // Check if value is a list or dict that needs block formatting
            var isContainer = value is Dictionary<string, object?> || 
                             (value is System.Collections.IEnumerable enumerable && !(value is string));
            if (isContainer)
            {
                lines.Add(prefix);
                WriteValue(value, level + indent, lines);
            }
            else if (IsInline(value))
            {
                lines.Add($"{prefix} {FormatScalar(value)}");
            }
            else
            {
                lines.Add(prefix);
                WriteValue(value, level + indent, lines);
            }
        }
    }

    private void WriteArray(List<object?> seq, int level, List<string> lines)
    {
        // Arrays are always written with "-" prefix, not as tabular
        // Tabular format is only used when array is a value in an object
        
        // Use parallel processing for large arrays of simple values (threshold: 200 items)
        // Optimized based on benchmark results showing good performance at 1000 items
        const int parallelThreshold = 200;
        if (seq.Count >= parallelThreshold && seq.All(IsInline))
        {
            // Fast path: all items are inline, can process in parallel
            var prefix = new string(' ', level) + "-";
            var itemLines = new string[seq.Count];
            Parallel.For(0, seq.Count, i =>
            {
                var item = seq[i];
                itemLines[i] = $"{prefix} {FormatScalar(item)}";
            });
            lines.AddRange(itemLines);
        }
        else
        {
            // Sequential processing for small arrays or arrays with complex items
            foreach (var item in seq)
            {
                var prefix = new string(' ', level) + "-";
                if (IsInline(item))
                {
                    lines.Add($"{prefix} {FormatScalar(item)}");
                }
                else
                {
                    lines.Add(prefix);
                    WriteValue(item, level + indent, lines);
                }
            }
        }
    }

    private void WriteTableAsKey(string key, List<Dictionary<string, object?>> rows, TabularSchema schema, int level, List<string> lines)
    {
        var fields = string.Join(",", schema.Keys);
        var header = new string(' ', level) + $"{key}[{rows.Count}]{{{fields}}}:";
        lines.Add(header);

        // Use parallel processing for large tables (threshold: 50 rows)
        // Optimized based on benchmark results showing good performance at 200 rows
        const int parallelThreshold = 50;
        if (rows.Count >= parallelThreshold)
        {
            var rowLines = new string[rows.Count];
            var indentStr = new string(' ', level + indent);
            Parallel.For(0, rows.Count, i =>
            {
                var row = rows[i];
                var cells = new List<string>(schema.Keys.Count);
                foreach (var k in schema.Keys)
                {
                    var value = row.GetValueOrDefault(k);
                    cells.Add(FormatScalar(value));
                }
                rowLines[i] = indentStr + string.Join(",", cells);
            });
            lines.AddRange(rowLines);
        }
        else
        {
            // Sequential processing for small tables (less overhead)
            foreach (var row in rows)
            {
                var cells = new List<string>();
                foreach (var k in schema.Keys)
                {
                    var value = row.GetValueOrDefault(k);
                    cells.Add(FormatScalar(value));
                }
                var rowLine = new string(' ', level + indent) + string.Join(",", cells);
                lines.Add(rowLine);
            }
        }
    }

    private bool IsInline(object? value)
    {
        if (value == null)
        {
            return true;
        }

        // Lists and dictionaries are never inline (they need block formatting)
        if (value is Dictionary<string, object?> || value is List<object?>)
        {
            return false;
        }

        if (value is string str && str.Contains('\n'))
        {
            return false;
        }

        return true;
    }

    private string? InlineContainerRepr(object? value)
    {
        // Only return inline representation for empty containers
        // Non-empty containers should use block formatting
        if (value is Dictionary<string, object?> dict && dict.Count == 0)
        {
            return "{}";
        }

        if (value is List<object?> list && list.Count == 0)
        {
            return "[]";
        }

        return null;
    }

    private string FormatScalar(object? value)
    {
        return Utils.FormatScalar(value);
    }
}

