using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ToonSharp;

/// <summary>
/// Represents a single line of TOON source code.
/// </summary>
public class Line
{
    public int Indent { get; set; }
    public string Content { get; set; }
    public int LineNo { get; set; }

    public Line(int indent, string content, int lineNo)
    {
        Indent = indent;
        Content = content;
        LineNo = lineNo;
    }
}

/// <summary>
/// Lexer that tokenizes TOON source into lines with indentation tracking.
/// </summary>
public class ToonLexer
{
    private readonly string source;

    public ToonLexer(string source)
    {
        this.source = source.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public List<Line> IterLines()
    {
        var text = RemoveBlockComments(source);
        var lines = new List<Line>();
        var rawLines = text.Split('\n');

        for (int idx = 0; idx < rawLines.Length; idx++)
        {
            var raw = rawLines[idx];
            var stripped = StripInlineComment(raw);
            if (string.IsNullOrWhiteSpace(stripped))
            {
                continue;
            }

            var strippedSpan = stripped.AsSpan();
            var leading = 0;
            while (leading < strippedSpan.Length && (strippedSpan[leading] == ' ' || strippedSpan[leading] == '\t'))
            {
                if (strippedSpan[leading] == '\t')
                {
                    throw new ToonSyntaxError("Tabs are not allowed for indentation", idx + 1, 1);
                }
                leading++;
            }
            
            var indent = leading;
            var content = strippedSpan.Slice(leading).TrimEnd().ToString();
            lines.Add(new Line(indent, content, idx + 1));
        }

        return lines;
    }

    private static string RemoveBlockComments(string text)
    {
        var result = new StringBuilder();
        int depth = 0;
        int i = 0;

        while (i < text.Length)
        {
            if (i < text.Length - 1 && text.Substring(i, 2) == "/*")
            {
                depth++;
                i += 2;
                continue;
            }

            if (depth > 0)
            {
                if (i < text.Length - 1 && text.Substring(i, 2) == "*/")
                {
                    depth--;
                    i += 2;
                    continue;
                }
                result.Append(text[i] == '\n' ? '\n' : ' ');
                i++;
                continue;
            }

            result.Append(text[i]);
            i++;
        }

        if (depth != 0)
        {
            throw new ToonSyntaxError("Unterminated block comment");
        }

        return result.ToString();
    }

    private static string StripInlineComment(string line)
    {
        var buf = new StringBuilder();
        bool inString = false;
        bool escape = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];

            if (escape)
            {
                buf.Append(ch);
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                buf.Append(ch);
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                buf.Append(ch);
                inString = !inString;
                continue;
            }

            if (!inString && (ch == '#' || (i < line.Length - 1 && line.Substring(i, 2) == "//")))
            {
                break;
            }

            buf.Append(ch);
        }

        return buf.ToString().TrimEnd();
    }
}

/// <summary>
/// Parser that converts TOON text into .NET objects.
/// </summary>
public class ToonParser
{
    private readonly string mode;
    private List<Line> lines = new();
    private int currentIndex = 0;
    private static readonly Regex FoldableSegmentRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private const string DefaultTableDelimiter = ",";

    public ToonParser(string mode = "strict")
    {
        this.mode = mode;
    }

    public object? Parse(string source)
    {
        var lexer = new ToonLexer(source);
        lines = lexer.IterLines();
        currentIndex = 0;

        if (lines.Count == 0)
        {
            return null;
        }

        // Always start by parsing as a value (which can be object, array, or scalar)
        // The value parser will handle tables correctly
        return ParseValue(0);
    }

    private object? ParseValue(int expectedIndent)
    {
        if (currentIndex >= lines.Count)
        {
            return null;
        }

        var line = lines[currentIndex];
        if (line.Indent < expectedIndent)
        {
            return null;
        }

        if (line.Indent > expectedIndent)
        {
            // This line belongs to a parent structure
            return null;
        }

        // Check for array item first
        if (line.Content.StartsWith("-"))
        {
            return ParseArray(expectedIndent);
        }

        // Check for key-value pair or object (including tables)
        // Tables use key[N]{fields}: syntax which contains ":"
        if (line.Content.Contains(":"))
        {
            return ParseObject(expectedIndent);
        }

        if (mode == "strict" && LooksLikeMissingColon(line.Content))
        {
            throw new ToonSyntaxError($"Expected ':' after key-like token: {line.Content.Trim()}", line.LineNo);
        }

        // Scalar value (standalone)
        var scalar = ParseScalar(line.Content);
        currentIndex++;
        return scalar;
    }

    private Dictionary<string, object?> ParseObject(int expectedIndent)
    {
        var result = new Dictionary<string, object?>();
        int baseIndent = expectedIndent;

        while (currentIndex < lines.Count)
        {
            var line = lines[currentIndex];
            if (line.Indent < baseIndent)
            {
                break;
            }

            if (line.Indent != baseIndent)
            {
                // This line belongs to a nested structure, skip it
                currentIndex++;
                continue;
            }

            // Check for table syntax first: key[N]{fields}:
            var tableHeader = ParseTableHeader(line.Content);
            if (tableHeader != null)
            {
                var tableValue = ParseTableFromHeader(currentIndex, tableHeader.Fields, tableHeader.Count, tableHeader.Delimiter, baseIndent);
                AssignValue(result, tableHeader.Key, tableValue.rows, tableHeader.AllowPathExpansion, line.LineNo);
                currentIndex = tableValue.nextIndex;
                continue;
            }

            if (!line.Content.Contains(":"))
            {
                // Not a key-value pair, might be an error in strict mode
                if (mode == "strict")
                {
                    throw new ToonSyntaxError($"Expected key-value pair, got: {line.Content}", line.LineNo);
                }
                break;
            }

            var tokenResult = SplitKeyValueToken(line.Content);
            if (tokenResult == null)
            {
                if (mode == "strict")
                {
                    throw new ToonSyntaxError($"Invalid key-value syntax: {line.Content}", line.LineNo);
                }
                currentIndex++;
                continue;
            }

            var (keyToken, valueStr) = tokenResult.Value;
            var key = keyToken.Clean;
            InlineArrayInfo? inlineArray = !keyToken.WasQuoted ? TryParseInlineArrayKey(key) : null;
            var targetKey = inlineArray?.BaseKey ?? key;
            var allowPathExpansion = !keyToken.WasQuoted;
            var treatAsInlineArray = inlineArray.HasValue && (!string.IsNullOrWhiteSpace(valueStr) || inlineArray.Value.Count == 0);

            currentIndex++;
            object? value;

            if (treatAsInlineArray)
            {
                value = ParseInlineArrayValues(valueStr, inlineArray!.Value.Count, line.LineNo);
            }
            // Check if next line is indented (block value)
            else if (currentIndex < lines.Count && lines[currentIndex].Indent > baseIndent)
            {
                var childIndent = lines[currentIndex].Indent;
                value = ParseValue(childIndent);
            }
            else if (!string.IsNullOrWhiteSpace(valueStr))
            {
                value = ParseScalar(valueStr.Trim());
            }
            else
            {
                // Empty value after colon - might be an error
                if (mode == "strict" && currentIndex >= lines.Count)
                {
                    throw new ToonSyntaxError($"Missing value for key: {key}", line.LineNo);
                }
                value = null;
            }

            AssignValue(result, targetKey, value, allowPathExpansion, line.LineNo);
        }

        return result;
    }

    private List<object?> ParseArray(int expectedIndent)
    {
        var result = new List<object?>();
        int baseIndent = expectedIndent;

        while (currentIndex < lines.Count)
        {
            var line = lines[currentIndex];
            if (line.Indent < baseIndent)
            {
                break;
            }

            if (line.Indent != baseIndent || !line.Content.StartsWith("-"))
            {
                currentIndex++;
                continue;
            }

            var content = line.Content.Substring(1).TrimStart();
            currentIndex++;

            object? value;
            if (currentIndex < lines.Count && lines[currentIndex].Indent > baseIndent)
            {
                value = ParseValue(baseIndent + 2);
            }
            else
            {
                value = ParseScalar(content);
            }

            result.Add(value);
        }

        return result;
    }

    private TableHeaderInfo? ParseTableHeader(string content)
    {
        content = content.Trim();
        if (!content.EndsWith(":", StringComparison.Ordinal))
        {
            return null;
        }

        int bracketStart = content.IndexOf('[');
        int bracketEnd = content.IndexOf(']', bracketStart + 1);
        int braceStart = content.IndexOf('{', bracketEnd + 1);
        int braceEnd = content.IndexOf('}', braceStart + 1);

        if (bracketStart < 0 || bracketEnd < 0 || braceStart < 0 || braceEnd < 0 || braceEnd > content.Length - 2)
        {
            return null;
        }

        var rawKey = content.Substring(0, bracketStart).Trim();
        if (string.IsNullOrEmpty(rawKey))
        {
            return null;
        }

        var wasQuoted = rawKey.Length >= 2 && rawKey.StartsWith('"') && rawKey.EndsWith('"');
        var cleanKey = UnquoteKey(rawKey);

        var bracketSegment = content.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
        var digitPart = new string(bracketSegment.TakeWhile(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitPart) || !int.TryParse(digitPart, out var count))
        {
            return null;
        }

        var delimiterPart = bracketSegment.Substring(digitPart.Length);
        var delimiter = string.IsNullOrEmpty(delimiterPart) ? DefaultTableDelimiter : delimiterPart;

        var fieldsSegment = content.Substring(braceStart + 1, braceEnd - braceStart - 1);
        var fields = SplitEscapedRow(fieldsSegment, delimiter)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        if (fields.Count == 0)
        {
            return null;
        }

        return new TableHeaderInfo(cleanKey, !wasQuoted, count, fields, delimiter);
    }

    private (List<Dictionary<string, object?>> rows, int nextIndex) ParseTableFromHeader(int start, List<string> fields, int expectedLength, string delimiter, int indent)
    {
        var headerLine = lines[start];
        int index = start + 1;
        
        // First, collect all table row lines
        var tableLines = new List<(Line line, int originalIndex)>();
        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Indent <= indent)
            {
                break;
            }
            tableLines.Add((line, index));
            index++;
        }

        var rows = new List<Dictionary<string, object?>>(tableLines.Count);
        
        // Use parallel processing for large tables (threshold: 50 rows)
        // Optimized based on benchmark results showing good performance at 200 rows
        const int parallelThreshold = 50;
        if (tableLines.Count >= parallelThreshold)
        {
            var parsedRows = new Dictionary<string, object?>[tableLines.Count];
            Parallel.For(0, tableLines.Count, i =>
            {
                var (line, _) = tableLines[i];
                
                // Parse delimited values
                var values = SplitEscapedRow(line.Content.Trim(), delimiter);
                if (values.Count == 0)
                {
                    // Fallback: simple split
                    values = line.Content
                        .Trim()
                        .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim())
                        .ToList();
                }

                if (values.Count != fields.Count)
                {
                    throw new ToonSyntaxError(
                        $"Expected {fields.Count} values in table row, got {values.Count}",
                        line.LineNo,
                        1);
                }

                var row = new Dictionary<string, object?>();
                for (int j = 0; j < fields.Count; j++)
                {
                    row[fields[j]] = ParseScalar(values[j]);
                }
                parsedRows[i] = row;
            });
            rows.AddRange(parsedRows);
        }
        else
        {
            // Sequential processing for small tables (less overhead)
            foreach (var (line, _) in tableLines)
            {
                // Parse delimited values
                var values = SplitEscapedRow(line.Content.Trim(), delimiter);
                if (values.Count == 0)
                {
                    // Fallback: simple split
                    values = line.Content
                        .Trim()
                        .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim())
                        .ToList();
                }

                if (values.Count != fields.Count)
                {
                    throw new ToonSyntaxError(
                        $"Expected {fields.Count} values in table row, got {values.Count}",
                        line.LineNo,
                        1);
                }

                var row = new Dictionary<string, object?>();
                for (int i = 0; i < fields.Count; i++)
                {
                    row[fields[i]] = ParseScalar(values[i]);
                }
                rows.Add(row);
            }
        }

        // Validate that we parsed the expected number of rows
        if (rows.Count != expectedLength)
        {
            throw new ToonSyntaxError(
                $"Table header declares {expectedLength} rows, but found {rows.Count} rows",
                headerLine.LineNo,
                1);
        }

        return (rows, index);
    }

    private List<string> SplitEscapedRow(string line, string separator)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        bool escape = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];

            if (escape)
            {
                current.Append(ch);
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                current.Append(ch);
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                current.Append(ch);
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && i + separator.Length <= line.Length && 
                line.AsSpan(i, separator.Length).SequenceEqual(separator.AsSpan()))
            {
                result.Add(current.ToString().Trim());
                current.Clear();
                i += separator.Length - 1;
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString().Trim());
        }

        return result;
    }

    private (KeyToken token, string? value)? SplitKeyValueToken(string line)
    {
        int colonIndex = line.IndexOf(':');
        if (colonIndex < 0)
        {
            return null;
        }

        var lineSpan = line.AsSpan();
        var rawKey = lineSpan.Slice(0, colonIndex).TrimEnd().ToString();
        var value = colonIndex < lineSpan.Length - 1 ? lineSpan.Slice(colonIndex + 1).TrimStart().ToString() : null;
        var wasQuoted = rawKey.Length >= 2 && rawKey.StartsWith('"') && rawKey.EndsWith('"');
        var cleanKey = UnquoteKey(rawKey);
        return (new KeyToken(rawKey, cleanKey, wasQuoted), value);
    }

    private InlineArrayInfo? TryParseInlineArrayKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !key.EndsWith("]", StringComparison.Ordinal))
        {
            return null;
        }

        int bracketIndex = key.LastIndexOf('[');
        if (bracketIndex < 0)
        {
            return null;
        }

        var keySpan = key.AsSpan();
        var countSegment = keySpan.Slice(bracketIndex + 1, keySpan.Length - bracketIndex - 2);
        if (!int.TryParse(countSegment, out var count))
        {
            return null;
        }

        var baseKey = keySpan.Slice(0, bracketIndex).TrimEnd().ToString();
        if (string.IsNullOrWhiteSpace(baseKey))
        {
            return null;
        }

        return new InlineArrayInfo(baseKey, count);
    }

    private List<object?> ParseInlineArrayValues(string? valueSegment, int expectedCount, int lineNo)
    {
        var tokens = string.IsNullOrWhiteSpace(valueSegment)
            ? new List<string>()
            : SplitEscapedRow(valueSegment.Trim(), DefaultTableDelimiter);

        if (tokens.Count != expectedCount)
        {
            throw new ToonSyntaxError(
                $"Inline array declares {expectedCount} values but found {tokens.Count}",
                lineNo);
        }

        var result = new List<object?>();
        foreach (var token in tokens)
        {
            result.Add(ParseScalar(token));
        }

        return result;
    }

    private void AssignValue(Dictionary<string, object?> target, string key, object? value, bool allowPathExpansion, int? lineNo)
    {
        if (!allowPathExpansion || !key.Contains('.'))
        {
            target[key] = value;
            return;
        }

        // Use Span-based splitting for better performance
        var segments = new List<string>();
        var keySpan = key.AsSpan();
        int start = 0;
        for (int i = 0; i <= keySpan.Length; i++)
        {
            if (i == keySpan.Length || keySpan[i] == '.')
            {
                if (i > start)
                {
                    segments.Add(keySpan.Slice(start, i - start).ToString());
                }
                start = i + 1;
            }
        }
        
        if (segments.Count == 0)
        {
            target[key] = value;
            return;
        }

        var current = target;
        for (int i = 0; i < segments.Count - 1; i++)
        {
            var segment = segments[i];
            EnsureFoldableSegment(segment, lineNo);

            if (!current.TryGetValue(segment, out var existing))
            {
                var next = new Dictionary<string, object?>();
                current[segment] = next;
                current = next;
            }
            else if (existing is Dictionary<string, object?> nested)
            {
                current = nested;
            }
            else
            {
                throw new ToonSyntaxError($"Path '{key}' conflicts with existing value at '{segment}'", lineNo);
            }
        }

        var finalSegment = segments[^1];
        EnsureFoldableSegment(finalSegment, lineNo);
        if (current.TryGetValue(finalSegment, out var existingFinal) &&
            existingFinal is Dictionary<string, object?> &&
            value is not Dictionary<string, object?>)
        {
            throw new ToonSyntaxError($"Path '{key}' conflicts with existing nested object", lineNo);
        }
        current[finalSegment] = value;
    }

    private static void EnsureFoldableSegment(string segment, int? lineNo)
    {
        if (!FoldableSegmentRegex.IsMatch(segment))
        {
            throw new ToonSyntaxError($"Invalid path segment '{segment}' for key folding", lineNo);
        }
    }

    private static bool LooksLikeMissingColon(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var trimmed = content.Trim();
        if (trimmed.StartsWith("-", StringComparison.Ordinal) || trimmed.StartsWith("\"", StringComparison.Ordinal))
        {
            return false;
        }

        var separatorIndex = trimmed.IndexOfAny(new[] { ' ', '\t' });
        if (separatorIndex <= 0)
        {
            return false;
        }

        var candidateKey = trimmed.Substring(0, separatorIndex);
        return FoldableSegmentRegex.IsMatch(candidateKey);
    }

    private sealed class TableHeaderInfo
    {
        public TableHeaderInfo(string key, bool allowPathExpansion, int count, List<string> fields, string delimiter)
        {
            Key = key;
            AllowPathExpansion = allowPathExpansion;
            Count = count;
            Fields = fields;
            Delimiter = delimiter;
        }

        public string Key { get; }
        public bool AllowPathExpansion { get; }
        public int Count { get; }
        public List<string> Fields { get; }
        public string Delimiter { get; }
    }

    private readonly struct InlineArrayInfo
    {
        public InlineArrayInfo(string baseKey, int count)
        {
            BaseKey = baseKey;
            Count = count;
        }

        public string BaseKey { get; }
        public int Count { get; }
    }

    private readonly struct KeyToken
    {
        public KeyToken(string raw, string clean, bool wasQuoted)
        {
            Raw = raw;
            Clean = clean;
            WasQuoted = wasQuoted;
        }

        public string Raw { get; }
        public string Clean { get; }
        public bool WasQuoted { get; }
    }

    private string UnquoteKey(string key)
    {
        if (key.StartsWith('"') && key.EndsWith('"'))
        {
            return JsonSerializer.Deserialize<string>(key) ?? key;
        }
        return key;
    }

    private object? ParseScalar(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        content = content.Trim();

        // Empty containers
        if (content == "[]") return new List<object?>();
        if (content == "{}") return new Dictionary<string, object?>();

        // Boolean and null
        if (content == "true") return true;
        if (content == "false") return false;
        if (content == "null") return null;

        // Number
        var number = Utils.GuessNumber(content);
        if (number != null)
        {
            return number;
        }

        // String (quoted)
        if (content.StartsWith('"') && content.EndsWith('"'))
        {
            try
            {
                return JsonSerializer.Deserialize<string>(content);
            }
            catch
            {
                // If JSON deserialization fails, return as-is (might be invalid)
                return content;
            }
        }

        // Treat any remaining token as a bare string literal (per TOON examples)
        return content;
    }
}

