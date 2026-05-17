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
        var textSpan = text.AsSpan();

        // Pre-estimate valid lines for exact capacity to avoid resizes
        int estimatedLines = EstimateValidLines(textSpan);
        var lines = new List<Line>(estimatedLines);
        int lineNumber = 1;
        int start = 0;

        for (int i = 0; i <= textSpan.Length; i++)
        {
            if (i == textSpan.Length || textSpan[i] == '\n')
            {
                ProcessLineOptimized(textSpan.Slice(start, i - start), lineNumber, lines);
                start = i + 1;
                lineNumber++;
            }
        }

        return lines;
    }

    private void ProcessLineOptimized(ReadOnlySpan<char> raw, int lineNumber, List<Line> lines)
    {
        // Strip inline comment using Span
        var stripped = StripInlineCommentSpan(raw);

        // Early exit for empty lines
        if (stripped.IsEmpty)
        {
            return;
        }

        // Count leading spaces and check for tabs (only at the start for indentation)
        int indent = 0;
        int firstNonSpace = -1;

        for (int i = 0; i < stripped.Length; i++)
        {
            char c = stripped[i];

            if (c == ' ')
            {
                if (firstNonSpace == -1)
                {
                    indent++;
                }
            }
            else if (c == '\t')
            {
                // Only check for tabs at the start (for indentation), not in content
                if (firstNonSpace == -1)
                {
                    throw new ToonSyntaxError("Tabs are not allowed for indentation", lineNumber, 1);
                }
                // Tabs in content are allowed (e.g., as delimiters in tables)
            }
            else
            {
                if (firstNonSpace == -1)
                {
                    firstNonSpace = i;
                }
            }
        }

        // If only spaces, exit
        if (firstNonSpace == -1)
        {
            return;
        }

        // Extract content and do manual TrimEnd
        var content = stripped.Slice(indent);
        int end = content.Length - 1;

        while (end >= 0 && char.IsWhiteSpace(content[end]))
        {
            end--;
        }

        if (end < 0)
        {
            return;
        }

        var finalContent = content.Slice(0, end + 1);
        lines.Add(new Line(indent, finalContent.ToString(), lineNumber));
    }

    private static int EstimateValidLines(ReadOnlySpan<char> text)
    {
        int count = 0;
        int lineStart = 0;

        for (int i = 0; i <= text.Length; i++)
        {
            if (i == text.Length || text[i] == '\n')
            {
                var line = text.Slice(lineStart, i - lineStart);

                // Quick estimation: if not empty after checking for non-whitespace
                if (!IsAllWhiteSpace(line))
                {
                    count++;
                }

                lineStart = i + 1;
            }
        }

        return count > 0 ? count : 16; // Minimum 16 to avoid small resizes
    }

    private static bool IsAllWhiteSpace(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (!char.IsWhiteSpace(span[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static ReadOnlySpan<char> StripInlineCommentSpan(ReadOnlySpan<char> line)
    {
        // Search for '#' or '//' outside of strings
        bool inString = false;
        bool escape = false;
        int commentStart = -1;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                // Check for '#' comment
                if (c == '#')
                {
                    commentStart = i;
                    break;
                }
                // Check for '//' comment
                if (i < line.Length - 1 && line.Slice(i, 2).SequenceEqual("//".AsSpan()))
                {
                    commentStart = i;
                    break;
                }
            }
        }

        if (commentStart >= 0)
        {
            return line.Slice(0, commentStart);
        }

        return line;
    }

    private static string RemoveBlockComments(string text)
    {
        var result = new StringBuilder();
        int depth = 0;
        int i = 0;

        while (i < text.Length)
        {
            if (i < text.Length - 1 && text.AsSpan(i, 2).SequenceEqual("/*"))
            {
                depth++;
                i += 2;
                continue;
            }

            if (depth > 0)
            {
                if (i < text.Length - 1 && text.AsSpan(i, 2).SequenceEqual("*/"))
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

            if (!inString && (ch == '#' || (i < line.Length - 1 && line.AsSpan(i, 2).SequenceEqual("//"))))
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
    private readonly bool expandPathsSafe;
    private List<Line> lines = new();
    private int currentIndex = 0;
    private int? cachedIndentSize;
    private static readonly Regex FoldableSegmentRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private const string DefaultTableDelimiter = ",";

    public ToonParser(string mode = "strict")
        : this(new ToonDecodeOptions { Strict = string.Equals(mode, "strict", StringComparison.OrdinalIgnoreCase) })
    {
    }

    public ToonParser(ToonDecodeOptions options)
    {
        mode = options.ParserMode;
        expandPathsSafe = options.ExpandPathsSafe;
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
        // Optimization 1: Direct access without additional bounds check
        if (currentIndex >= lines.Count)
        {
            return null;
        }

        var line = lines[currentIndex];

        // Optimization 2: Simplified indent comparisons
        var indentDiff = line.Indent - expectedIndent;
        if (indentDiff != 0)
        {
            return null; // Both < and > return null
        }

        // Optimization 3: Cache content as span once
        var contentSpan = line.Content.AsSpan();

        // Early exit for empty content (edge case)
        if (contentSpan.IsEmpty)
        {
            currentIndex++;
            return null;
        }

        // Optimization 4: Check first character first (most common case)
        char firstChar = contentSpan[0];

        // Check for array item (very common case)
        if (firstChar == '-')
        {
            return ParseArray(expectedIndent);
        }

        // Optimization 5: IndexOf on Span (faster than Contains on string)
        int colonIndex = contentSpan.IndexOf(':');

        if (colonIndex >= 0)
        {
            // It's a key-value pair or table
            return ParseObject(expectedIndent);
        }

        // Strict mode verification
        if (mode == "strict" && LooksLikeMissingColonSpan(contentSpan))
        {
            throw new ToonSyntaxError(
                $"Expected ':' after key-like token: {contentSpan.Trim().ToString()}",
                line.LineNo);
        }

        // Scalar value (standalone)
        var scalar = ParseScalarSpan(contentSpan);
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
            var allowPathExpansion = expandPathsSafe && !keyToken.WasQuoted;
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
                value = ParseScalarSpan(valueStr.AsSpan().Trim());
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
            var headerLineIndex = currentIndex;
            currentIndex++;

            object? value;
            if (content.Length == 0)
            {
                value = new Dictionary<string, object?>();
            }
            else
            {
                var tableHeader = ParseTableHeader(content);
                if (tableHeader != null && content.IndexOf('{') >= 0)
                {
                    value = ParseListItemWithTabularFirstField(tableHeader, baseIndent, headerLineIndex);
                }
                else if (content.Contains(':'))
                {
                    value = ParseListItemWithFirstFieldOnHyphen(content, baseIndent);
                }
                else if (currentIndex < lines.Count && lines[currentIndex].Indent > baseIndent)
                {
                    value = ParseValue(baseIndent + DetectIndentSize() * 2);
                }
                else
                {
                    value = ParseScalarSpan(content.AsSpan());
                }
            }

            result.Add(value);
        }

        return result;
    }

    private Dictionary<string, object?> ParseListItemWithTabularFirstField(TableHeaderInfo header, int baseIndent, int headerLineIndex)
    {
        var step = DetectIndentSize();
        var rowIndent = baseIndent + step * 2;
        var siblingIndent = baseIndent + step;
        var (rows, nextIndex) = ParseTableFromHeader(
            headerLineIndex,
            header.Fields,
            header.Count,
            header.Delimiter,
            baseIndent,
            rowIndent,
            siblingIndent);
        currentIndex = nextIndex;

        var obj = new Dictionary<string, object?> { [header.Key] = rows };
        MergeListItemSiblingFields(obj, siblingIndent, baseIndent);
        return obj;
    }

    private Dictionary<string, object?> ParseListItemWithFirstFieldOnHyphen(string content, int baseIndent)
    {
        var tokenResult = SplitKeyValueToken(content);
        if (tokenResult == null)
        {
            return new Dictionary<string, object?>();
        }

        var (keyToken, valueStr) = tokenResult.Value;
        var key = keyToken.Clean;
        InlineArrayInfo? inlineArray = !keyToken.WasQuoted ? TryParseInlineArrayKey(key) : null;
        var targetKey = inlineArray?.BaseKey ?? key;
        var allowPathExpansion = expandPathsSafe && !keyToken.WasQuoted;
        var treatAsInlineArray = inlineArray.HasValue &&
                                 (!string.IsNullOrWhiteSpace(valueStr) || inlineArray.Value.Count == 0);

        var obj = new Dictionary<string, object?>();
        object? firstValue;

        var hyphenLineNo = lines[currentIndex - 1].LineNo;

        if (treatAsInlineArray)
        {
            firstValue = ParseInlineArrayValues(valueStr, inlineArray!.Value.Count, hyphenLineNo);
        }
        else if (currentIndex < lines.Count && lines[currentIndex].Indent > baseIndent + DetectIndentSize())
        {
            firstValue = ParseValue(lines[currentIndex].Indent);
        }
        else if (!string.IsNullOrWhiteSpace(valueStr))
        {
            firstValue = ParseScalarSpan(valueStr.AsSpan().Trim());
        }
        else
        {
            firstValue = null;
        }

        AssignValue(obj, targetKey, firstValue, allowPathExpansion, hyphenLineNo);
        MergeListItemSiblingFields(obj, baseIndent + DetectIndentSize(), baseIndent);
        return obj;
    }

    private void MergeListItemSiblingFields(Dictionary<string, object?> obj, int fieldIndent, int listItemIndent)
    {
        while (currentIndex < lines.Count)
        {
            var line = lines[currentIndex];
            if (line.Indent < fieldIndent)
            {
                break;
            }

            if (line.Indent == listItemIndent && line.Content.StartsWith("-"))
            {
                break;
            }

            if (line.Indent != fieldIndent)
            {
                currentIndex++;
                continue;
            }

            if (!line.Content.Contains(":"))
            {
                break;
            }

            var tokenResult = SplitKeyValueToken(line.Content);
            if (tokenResult == null)
            {
                currentIndex++;
                continue;
            }

            var (keyToken, valueStr) = tokenResult.Value;
            var key = keyToken.Clean;
            InlineArrayInfo? inlineArray = !keyToken.WasQuoted ? TryParseInlineArrayKey(key) : null;
            var targetKey = inlineArray?.BaseKey ?? key;
            var allowPathExpansion = expandPathsSafe && !keyToken.WasQuoted;
            var treatAsInlineArray = inlineArray.HasValue &&
                                     (!string.IsNullOrWhiteSpace(valueStr) || inlineArray.Value.Count == 0);

            currentIndex++;
            object? value;

            if (treatAsInlineArray)
            {
                value = ParseInlineArrayValues(valueStr, inlineArray!.Value.Count, line.LineNo);
            }
            else if (currentIndex < lines.Count && lines[currentIndex].Indent > fieldIndent)
            {
                value = ParseValue(lines[currentIndex].Indent);
            }
            else if (!string.IsNullOrWhiteSpace(valueStr))
            {
                value = ParseScalarSpan(valueStr.AsSpan().Trim());
            }
            else
            {
                value = null;
            }

            AssignValue(obj, targetKey, value, allowPathExpansion, line.LineNo);
        }
    }

    private int DetectIndentSize()
    {
        if (cachedIndentSize.HasValue)
        {
            return cachedIndentSize.Value;
        }

        int gcd = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            int indent = lines[i].Indent;
            if (indent <= 0)
            {
                continue;
            }

            gcd = gcd == 0 ? indent : Gcd(gcd, indent);
        }

        cachedIndentSize = gcd > 0 ? gcd : 2;
        return cachedIndentSize.Value;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a;
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

        return new TableHeaderInfo(cleanKey, expandPathsSafe && !wasQuoted, count, fields, delimiter);
    }

    private (List<Dictionary<string, object?>> rows, int nextIndex) ParseTableFromHeader(
        int start,
        List<string> fields,
        int expectedLength,
        string delimiter,
        int headerIndent,
        int? rowIndentOverride = null,
        int? siblingIndentOverride = null)
    {
        var headerLine = lines[start];
        int index = start + 1;
        int step = DetectIndentSize();
        int rowIndent = rowIndentOverride ?? (index < lines.Count && lines[index].Indent > headerIndent
            ? lines[index].Indent
            : headerIndent + step);
        int siblingIndent = siblingIndentOverride ?? headerIndent + step;

        var tableLines = new List<(Line line, int originalIndex)>();
        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Indent <= headerIndent)
            {
                break;
            }

            if (siblingIndentOverride.HasValue && line.Indent == siblingIndent && line.Indent < rowIndent)
            {
                break;
            }

            if (line.Indent != rowIndent)
            {
                index++;
                continue;
            }

            tableLines.Add((line, index));
            index++;
        }

        var rows = new List<Dictionary<string, object?>>(tableLines.Count);
        
        // Use parallel processing for large tables (threshold: 50 rows)
        // Optimized based on benchmark results showing good performance at 200 rows
        const int parallelThreshold = 100;
        if (tableLines.Count >= parallelThreshold)
        {
            var parsedRows = new Dictionary<string, object?>[tableLines.Count];
            var fieldsArray = fields.ToArray();
            Parallel.For(0, tableLines.Count, i =>
            {
                var (line, _) = tableLines[i];
                parsedRows[i] = ParseTableRow(line, fieldsArray, delimiter);
            });
            rows.AddRange(parsedRows);
        }
        else
        {
            var fieldsArray = fields.ToArray();
            foreach (var (line, _) in tableLines)
            {
                rows.Add(ParseTableRow(line, fieldsArray, delimiter));
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

    private Dictionary<string, object?> ParseTableRow(Line line, string[] fields, string delimiter)
    {
        var content = line.Content.AsSpan().Trim();
        if (delimiter == DefaultTableDelimiter &&
            TryParseCommaDelimitedTableRow(content, fields, out var fastRow))
        {
            return fastRow;
        }

        var values = SplitEscapedRow(line.Content.Trim(), delimiter);
        if (values.Count == 0)
        {
            values = line.Content
                .Trim()
                .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .ToList();
        }

        if (values.Count != fields.Length)
        {
            throw new ToonSyntaxError(
                $"Expected {fields.Length} values in table row, got {values.Count}",
                line.LineNo,
                1);
        }

        var row = new Dictionary<string, object?>(fields.Length);
        for (int i = 0; i < fields.Length; i++)
        {
            row[fields[i]] = ParseScalarSpan(values[i].AsSpan());
        }

        return row;
    }

    private static bool TryParseCommaDelimitedTableRow(
        ReadOnlySpan<char> line,
        string[] fields,
        out Dictionary<string, object?> row)
    {
        row = null!;
        if (line.IsEmpty || line.IndexOf('"') >= 0)
        {
            return false;
        }

        var parsed = new Dictionary<string, object?>(fields.Length);
        int fieldIndex = 0;
        int start = 0;

        for (int i = 0; i <= line.Length; i++)
        {
            if (i < line.Length && line[i] != ',')
            {
                continue;
            }

            if (fieldIndex >= fields.Length)
            {
                return false;
            }

            parsed[fields[fieldIndex]] = ParseTableScalarToken(line.Slice(start, i - start));
            fieldIndex++;
            start = i + 1;
        }

        if (fieldIndex != fields.Length)
        {
            return false;
        }

        row = parsed;
        return true;
    }

    private static object? ParseTableScalarToken(ReadOnlySpan<char> token)
    {
        if (token.IsEmpty)
        {
            return null;
        }

        int s = 0;
        int e = token.Length - 1;
        while (s <= e && token[s] == ' ')
        {
            s++;
        }

        while (e >= s && token[e] == ' ')
        {
            e--;
        }

        if (s > e)
        {
            return null;
        }

        var trimmed = token.Slice(s, e - s + 1);
        if (trimmed.SequenceEqual("true"))
        {
            return true;
        }

        if (trimmed.SequenceEqual("false"))
        {
            return false;
        }

        if (trimmed.SequenceEqual("null"))
        {
            return null;
        }

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            return DecodeJsonStringLiteral(trimmed);
        }

        var number = Utils.GuessNumber(trimmed);
        if (number != null)
        {
            return number;
        }

        return trimmed.ToString();
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
                AddTrimmedToken(result, current);
                current.Clear();
                i += separator.Length - 1;
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
        {
            AddTrimmedToken(result, current);
        }

        return result;
    }

    private static void AddTrimmedToken(List<string> result, StringBuilder current)
    {
        if (current.Length == 0)
        {
            result.Add(string.Empty);
            return;
        }

        int start = 0;
        int end = current.Length - 1;
        while (start <= end && char.IsWhiteSpace(current[start]))
        {
            start++;
        }

        while (end >= start && char.IsWhiteSpace(current[end]))
        {
            end--;
        }

        if (start > end)
        {
            result.Add(string.Empty);
            return;
        }

        if (start == 0 && end == current.Length - 1)
        {
            result.Add(current.ToString());
            return;
        }

        result.Add(current.ToString(start, end - start + 1));
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
        int digitLength = 0;
        while (digitLength < countSegment.Length && char.IsDigit(countSegment[digitLength]))
        {
            digitLength++;
        }

        if (digitLength == 0 || !int.TryParse(countSegment.Slice(0, digitLength), out var count))
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
        var result = new List<object?>(expectedCount);

        if (string.IsNullOrWhiteSpace(valueSegment))
        {
            if (expectedCount != 0)
            {
                throw new ToonSyntaxError(
                    $"Inline array declares {expectedCount} values but found 0",
                    lineNo);
            }

            return result;
        }

        var span = valueSegment.AsSpan().Trim();
        if (TryParseCommaSeparatedJsonStrings(span, expectedCount, lineNo, result))
        {
            return result;
        }

        ParseDelimitedScalarsInto(span, DefaultTableDelimiter, expectedCount, lineNo, result);
        return result;
    }

    /// <summary>
    /// Fast path for inline arrays of JSON-quoted strings: "a","b","c" (common for large primitive arrays).
    /// </summary>
    private static bool TryParseCommaSeparatedJsonStrings(
        ReadOnlySpan<char> line,
        int expectedCount,
        int lineNo,
        List<object?> destination)
    {
        if (line.IsEmpty || line[0] != '"')
        {
            return false;
        }

        int pos = 0;
        for (int found = 0; found < expectedCount; found++)
        {
            if (pos >= line.Length || line[pos] != '"')
            {
                return false;
            }

            int end = pos + 1;
            while (end < line.Length)
            {
                char ch = line[end];
                if (ch == '\\')
                {
                    if (end + 1 >= line.Length)
                    {
                        return false;
                    }

                    end += 2;
                    continue;
                }

                if (ch == '"')
                {
                    break;
                }

                end++;
            }

            if (end >= line.Length)
            {
                return false;
            }

            destination.Add(DecodeJsonStringLiteral(line.Slice(pos, end - pos + 1)));
            pos = end + 1;

            if (found + 1 < expectedCount)
            {
                if (pos >= line.Length || line[pos] != ',')
                {
                    return false;
                }

                pos++;
            }
        }

        if (pos != line.Length)
        {
            throw new ToonSyntaxError(
                $"Inline array declares {expectedCount} values but found trailing content",
                lineNo);
        }

        return true;
    }

    private static string DecodeJsonStringLiteral(ReadOnlySpan<char> quoted)
    {
        if (quoted.Length < 2 || quoted[0] != '"' || quoted[^1] != '"')
        {
            return quoted.ToString();
        }

        var inner = quoted.Slice(1, quoted.Length - 2);
        if (!inner.Contains('\\'))
        {
            return inner.ToString();
        }

        try
        {
            return JsonSerializer.Deserialize<string>(quoted.ToString()) ?? quoted.ToString();
        }
        catch
        {
            return quoted.ToString();
        }
    }

    private void ParseDelimitedScalarsInto(
        ReadOnlySpan<char> line,
        string separator,
        int expectedCount,
        int lineNo,
        List<object?> destination)
    {
        if (separator == DefaultTableDelimiter &&
            TryParseCommaSeparatedJsonStrings(line, expectedCount, lineNo, destination))
        {
            return;
        }

        int found = 0;
        int start = 0;
        bool inQuotes = false;
        bool escape = false;

        for (int i = 0; i <= line.Length; i++)
        {
            if (i < line.Length)
            {
                char ch = line[i];

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

                if (!inQuotes && separator.Length == 1 && ch == separator[0])
                {
                    AddInlineScalarToken(line.Slice(start, i - start), destination);
                    found++;
                    start = i + 1;
                    continue;
                }

                if (!inQuotes && separator.Length > 1 && i + separator.Length <= line.Length &&
                    line.Slice(i, separator.Length).SequenceEqual(separator.AsSpan()))
                {
                    AddInlineScalarToken(line.Slice(start, i - start), destination);
                    found++;
                    start = i + separator.Length;
                    i += separator.Length - 1;
                    continue;
                }
            }

            if (i == line.Length)
            {
                if (start < line.Length || found > 0)
                {
                    AddInlineScalarToken(line.Slice(start, i - start), destination);
                    found++;
                }
            }
        }

        if (found != expectedCount)
        {
            throw new ToonSyntaxError(
                $"Inline array declares {expectedCount} values but found {found}",
                lineNo);
        }
    }

    private void AddInlineScalarToken(ReadOnlySpan<char> token, List<object?> destination)
    {
        if (token.IsEmpty)
        {
            destination.Add(null);
            return;
        }

        int start = 0;
        int end = token.Length - 1;
        while (start <= end && token[start] == ' ')
        {
            start++;
        }

        while (end >= start && token[end] == ' ')
        {
            end--;
        }

        if (start > end)
        {
            destination.Add(null);
            return;
        }

        destination.Add(ParseScalarSpan(token.Slice(start, end - start + 1)));
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
            else if (string.Equals(mode, "permissive", StringComparison.OrdinalIgnoreCase))
            {
                var replacement = new Dictionary<string, object?>();
                current[segment] = replacement;
                current = replacement;
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
            if (string.Equals(mode, "permissive", StringComparison.OrdinalIgnoreCase))
            {
                current[finalSegment] = value;
                return;
            }

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

    // Span version of LooksLikeMissingColon (more efficient)
    private static bool LooksLikeMissingColonSpan(ReadOnlySpan<char> content)
    {
        var trimmed = content.Trim();

        if (trimmed.IsEmpty)
        {
            return false;
        }

        // Check if starts with "-" or quote (same as original)
        char first = trimmed[0];
        if (first == '-' || first == '"')
        {
            return false;
        }

        // Find separator (space or tab) - same logic as original
        int separatorIndex = -1;
        for (int i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] == ' ' || trimmed[i] == '\t')
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex <= 0)
        {
            return false;
        }

        // Get candidate key (part before separator)
        var candidateKey = trimmed.Slice(0, separatorIndex);

        // Must start with letter or underscore (same as FoldableSegmentRegex)
        if (candidateKey.IsEmpty)
        {
            return false;
        }

        char keyFirst = candidateKey[0];
        if (!char.IsLetter(keyFirst) && keyFirst != '_')
        {
            return false;
        }

        // Check if it matches the foldable segment pattern: [A-Za-z_][A-Za-z0-9_]*
        // This is equivalent to FoldableSegmentRegex.IsMatch()
        for (int i = 1; i < candidateKey.Length; i++)
        {
            char c = candidateKey[i];
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
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
        return ParseScalarSpan(content.AsSpan());
    }

    private object? ParseScalarSpan(ReadOnlySpan<char> content)
    {
        // Trim with Span (more efficient, no allocations)
        content = content.Trim();

        if (content.IsEmpty)
        {
            return null;
        }

        // Switch with Spans (C# 11+)
        if (content.SequenceEqual("[]"))
            return new List<object?>();
        if (content.SequenceEqual("{}"))
            return new Dictionary<string, object?>();
        if (content.SequenceEqual("true"))
            return true;
        if (content.SequenceEqual("false"))
            return false;
        if (content.SequenceEqual("null"))
            return null;

        // Check for quoted strings
        if (content.Length >= 2 && content[0] == '"' && content[^1] == '"')
        {
            return DecodeJsonStringLiteral(content);
        }

        // For numbers, use the optimized Span version
        var number = Utils.GuessNumber(content);
        if (number != null)
        {
            return number;
        }

        // Treat any remaining token as a bare string literal (per TOON examples)
        return content.ToString();
    }
}

