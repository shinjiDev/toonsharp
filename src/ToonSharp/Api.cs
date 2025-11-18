using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ToonSharp;

/// <summary>
/// Suggestion result for whether to use tabular format for an array.
/// </summary>
public class TabularSuggestion
{
    public bool UseTabular { get; set; }
    public int EstimatedSavings { get; set; }
    public List<string> Keys { get; set; } = new();

    public TabularSuggestion(bool useTabular, int estimatedSavings, List<string> keys)
    {
        UseTabular = useTabular;
        EstimatedSavings = estimatedSavings;
        Keys = keys;
    }
}

/// <summary>
/// Public API surface for ToonSharp.
/// </summary>
public static class Api
{
    /// <summary>
    /// Convert a .NET object to TOON format string.
    /// </summary>
    public static string ToToon(object? obj, int indent = 2, string mode = "auto")
    {
        var serializer = new ToonSerializer(indent, mode);
        return serializer.Dumps(obj);
    }

    /// <summary>
    /// Parse a TOON string into a .NET object.
    /// </summary>
    public static object? FromToon(string source, string mode = "strict")
    {
        var parser = new ToonParser(mode);
        return parser.Parse(source);
    }

    /// <summary>
    /// Validate a TOON string for syntax errors.
    /// </summary>
    public static (bool isValid, List<ValidationError> errors) ValidateToon(string source, bool strict = true)
    {
        var errors = new List<ValidationError>();
        try
        {
            var parser = new ToonParser(strict ? "strict" : "permissive");
            parser.Parse(source);
            return (true, errors);
        }
        catch (ToonSyntaxError ex)
        {
            errors.Add(new ValidationError(ex.Message, ex.Line, ex.Column, "error"));
            return (false, errors);
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError($"Parse error: {ex.Message}", null, null, "error"));
            return (false, errors);
        }
    }

    /// <summary>
    /// Suggest whether an array should use tabular format.
    /// </summary>
    public static TabularSuggestion SuggestTabular(List<object?> obj)
    {
        if (obj.Count == 0)
        {
            return new TabularSuggestion(false, 0, new List<string>());
        }

        if (!obj.All(item => item is Dictionary<string, object?>))
        {
            return new TabularSuggestion(false, 0, new List<string>());
        }

        var dictList = obj.Cast<Dictionary<string, object?>>().ToList();
        var schema = Utils.TabularSchema(dictList);

        if (schema == null)
        {
            return new TabularSuggestion(false, 0, new List<string>());
        }

        return new TabularSuggestion(schema.Savings > 0, schema.Savings, schema.Keys);
    }

    /// <summary>
    /// Stream JSON from input file to TOON output file.
    /// </summary>
    public static int StreamToToon(TextReader input, TextWriter output, int chunkSize = 65536, int indent = 2, string mode = "auto")
    {
        var jsonText = input.ReadToEnd();
        var obj = JsonSerializer.Deserialize<object>(jsonText);
        var toon = ToToon(obj, indent, mode);
        output.Write(toon);
        return Encoding.UTF8.GetByteCount(toon);
    }
}

