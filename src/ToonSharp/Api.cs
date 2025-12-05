using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Tomlyn;
using Tomlyn.Model;

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
        private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

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

    // ============================================
    // JSON Conversion Methods with Format Validation
    // ============================================

    /// <summary>
    /// Convert a JSON string to TOON format with format validation.
    /// Throws UnsupportedFormatException if the input is not valid JSON (e.g., XML).
    /// </summary>
    /// <param name="jsonSource">The JSON string to convert.</param>
    /// <param name="indent">Number of spaces for indentation (default: 2).</param>
    /// <param name="mode">Serialization mode: "auto", "compact", or "readable" (default: "auto").</param>
    /// <returns>The TOON formatted string.</returns>
    /// <exception cref="UnsupportedFormatException">Thrown when input is not valid JSON.</exception>
    public static string JsonToToon(string jsonSource, int indent = 2, string mode = "auto")
    {
        if (string.IsNullOrWhiteSpace(jsonSource))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(jsonSource));
        }

        // Detect unsupported formats before attempting to parse
        var detectedFormat = DetectFormat(jsonSource);
        if (detectedFormat != "JSON" && detectedFormat != "Unknown")
        {
            throw new UnsupportedFormatException(detectedFormat);
        }

        try
        {
            var obj = JsonSerializer.Deserialize<object>(jsonSource);
            return ToToon(obj, indent, mode);
        }
        catch (JsonException ex)
        {
            // If JSON parsing fails and we couldn't detect format earlier, check again
            var format = DetectFormat(jsonSource);
            if (format != "JSON" && format != "Unknown")
            {
                throw new UnsupportedFormatException(format);
            }
            throw new UnsupportedFormatException("Invalid JSON", ex.Message);
        }
    }

    /// <summary>
    /// Parse a JSON string into a .NET object with format validation.
    /// Throws UnsupportedFormatException if the input is not valid JSON.
    /// </summary>
    /// <param name="jsonSource">The JSON string to parse.</param>
    /// <returns>The parsed .NET object.</returns>
    /// <exception cref="UnsupportedFormatException">Thrown when input is not valid JSON.</exception>
    public static object? FromJson(string jsonSource)
    {
        if (string.IsNullOrWhiteSpace(jsonSource))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(jsonSource));
        }

        var detectedFormat = DetectFormat(jsonSource);
        if (detectedFormat != "JSON" && detectedFormat != "Unknown")
        {
            throw new UnsupportedFormatException(detectedFormat);
        }

        try
        {
            return JsonSerializer.Deserialize<object>(jsonSource);
        }
        catch (JsonException ex)
        {
            var format = DetectFormat(jsonSource);
            if (format != "JSON" && format != "Unknown")
            {
                throw new UnsupportedFormatException(format);
            }
            throw new UnsupportedFormatException("Invalid JSON", ex.Message);
        }
    }

    /// <summary>
    /// Detect the format of the input string.
    /// Returns "XML", "HTML", "JSON", "YAML", "TOML", "CSV", or "Unknown".
    /// </summary>
    public static string DetectFormat(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "Unknown";

        var trimmed = source.TrimStart();

        // HTML detection (must be before XML since HTML is a subset)
        if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            return "HTML";
        }

        // XML detection: starts with < and contains XML-like patterns
        if (trimmed.StartsWith('<'))
        {
            // Check for XML declaration or common XML patterns
            if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^<[a-zA-Z_][\w\-.:]*(\s|>|/>)"))
            {
                return "XML";
            }
        }

        // JSON detection: starts with { or [
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return "JSON";
        }

        // CSV detection: contains comma-separated values on first line with no special chars
        var firstLine = trimmed.Split('\n')[0].Trim();
        if (firstLine.Contains(',') && !firstLine.Contains(':') && !firstLine.Contains('{') && !firstLine.Contains('['))
        {
            var parts = firstLine.Split(',');
            if (parts.Length >= 2 && parts.All(p => !string.IsNullOrWhiteSpace(p)))
            {
                return "CSV";
            }
        }

        // INI detection: [section] pattern
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\[[^\]]+\]\s*$", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            // Could be TOML or INI - check for TOML-specific patterns
            if (trimmed.Contains("[[") || System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\w+\s*=\s*\[", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                return "TOML";
            }
        }

        return "Unknown";
    }

    /// <summary>
    /// Check if a format is supported by ToonSharp.
    /// </summary>
    public static bool IsFormatSupported(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "JSON" => true,
            "YAML" => true,
            "TOML" => true,
            "TOON" => true,
            _ => false
        };
    }

    /// <summary>
    /// Convert a YAML string to TOON format.
    /// </summary>
    public static string YamlToToon(string yamlSource, int indent = 2, string mode = "auto")
    {
        var obj = FromYaml(yamlSource);
        return ToToon(obj, indent, mode);
    }

    /// <summary>
    /// Convert a TOON string to YAML format.
    /// </summary>
    public static string ToonToYaml(string toonSource, string mode = "strict")
    {
        var obj = FromToon(toonSource, mode);
        return _yamlSerializer.Serialize(obj);
    }

    /// <summary>
    /// Convert a .NET object to YAML format string.
    /// </summary>
    public static string ToYaml(object? obj)
    {
        return _yamlSerializer.Serialize(obj);
    }

    /// <summary>
    /// Parse a YAML string into a .NET object.
    /// </summary>
    public static object? FromYaml(string yamlSource)
    {
        var obj = _yamlDeserializer.Deserialize<object>(yamlSource);
        return NormalizeYamlObject(obj);
    }

    /// <summary>
    /// Normalize YAML objects to standard .NET types compatible with TOON.
    /// </summary>
    private static object? NormalizeYamlObject(object? obj)
    {
        if (obj == null) return null;

        // Handle Dictionary<object, object> from YAML
        if (obj is Dictionary<object, object> yamlDict)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in yamlDict)
            {
                var key = kvp.Key?.ToString() ?? string.Empty;
                result[key] = NormalizeYamlObject(kvp.Value);
            }
            return result;
        }

        // Handle List<object> from YAML
        if (obj is List<object> yamlList)
        {
            var result = new List<object?>();
            foreach (var item in yamlList)
            {
                result.Add(NormalizeYamlObject(item));
            }
            return result;
        }

        // Handle arrays
        if (obj is Array array)
        {
            var result = new List<object?>();
            foreach (var item in array)
            {
                result.Add(NormalizeYamlObject(item));
            }
            return result;
        }

        // Return primitives as-is
        return obj;
    }

    // ============================================
    // TOML Conversion Methods
    // ============================================

    /// <summary>
    /// Convert TOML string to TOON format.
    /// </summary>
    public static string TomlToToon(string tomlSource, int indent = 2, string mode = "auto")
    {
        var obj = FromToml(tomlSource);
        return ToToon(obj, indent, mode);
    }

    /// <summary>
    /// Convert TOON string to TOML format.
    /// </summary>
    public static string ToonToToml(string toonSource)
    {
        var obj = FromToon(toonSource);
        return ToToml(obj);
    }

    /// <summary>
    /// Serialize a .NET object to TOML format.
    /// </summary>
    public static string ToToml(object? obj)
    {
        if (obj == null) return string.Empty;
        
        var tomlTable = ConvertToTomlTable(obj);
        return Toml.FromModel(tomlTable);
    }

    /// <summary>
    /// Deserialize TOML string to a .NET object.
    /// </summary>
    public static object? FromToml(string tomlSource)
    {
        var model = Toml.ToModel(tomlSource);
        return NormalizeTomlObject(model);
    }

    /// <summary>
    /// Convert .NET object to TOML table structure.
    /// </summary>
    private static TomlTable ConvertToTomlTable(object? obj)
    {
        var table = new TomlTable();
        
        if (obj == null) return table;

        if (obj is Dictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
            {
                table[kvp.Key] = ConvertToTomlValue(kvp.Value);
            }
        }
        else if (obj is IDictionary<string, object?> idict)
        {
            foreach (var kvp in idict)
            {
                table[kvp.Key] = ConvertToTomlValue(kvp.Value);
            }
        }

        return table;
    }

    /// <summary>
    /// Convert .NET value to TOML-compatible value.
    /// </summary>
    private static object? ConvertToTomlValue(object? value)
    {
        if (value == null) return null;

        // Handle dictionaries
        if (value is Dictionary<string, object?> dict)
        {
            return ConvertToTomlTable(dict);
        }

        // Handle lists/arrays
        if (value is List<object?> list)
        {
            var array = new TomlArray();
            foreach (var item in list)
            {
                var converted = ConvertToTomlValue(item);
                if (converted != null)
                {
                    array.Add(converted);
                }
            }
            return array;
        }

        // Handle JsonElement (from JSON deserialization)
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Object => ConvertToTomlTable(JsonElementToDictionary(jsonElement)),
                JsonValueKind.Array => ConvertToTomlValue(JsonElementToList(jsonElement)),
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetInt64(out var l) ? l : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value
            };
        }

        // Return primitives as-is
        return value;
    }

    /// <summary>
    /// Normalize TOML objects to standard .NET types compatible with TOON.
    /// </summary>
    private static object? NormalizeTomlObject(object? obj)
    {
        if (obj == null) return null;

        // Handle TomlTable
        if (obj is TomlTable tomlTable)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in tomlTable)
            {
                result[kvp.Key] = NormalizeTomlObject(kvp.Value);
            }
            return result;
        }

        // Handle TomlArray
        if (obj is TomlArray tomlArray)
        {
            var result = new List<object?>();
            foreach (var item in tomlArray)
            {
                result.Add(NormalizeTomlObject(item));
            }
            return result;
        }

        // Handle arrays
        if (obj is Array array)
        {
            var result = new List<object?>();
            foreach (var item in array)
            {
                result.Add(NormalizeTomlObject(item));
            }
            return result;
        }

        // Return primitives as-is
        return obj;
    }

    /// <summary>
    /// Helper method to convert JsonElement to Dictionary.
    /// </summary>
    private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = JsonElementToObject(prop.Value);
        }
        return dict;
    }

    /// <summary>
    /// Helper method to convert JsonElement to List.
    /// </summary>
    private static List<object?> JsonElementToList(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(JsonElementToObject(item));
        }
        return list;
    }

    /// <summary>
    /// Helper method to convert JsonElement to object.
    /// </summary>
    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => JsonElementToList(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }
}

