namespace ToonSharp;

/// <summary>
/// Base class for all ToonSharp exceptions.
/// All exceptions raised by ToonSharp inherit from this class.
/// </summary>
public class ToonError : Exception
{
    public ToonError(string message) : base(message) { }
    public ToonError(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when TOON input does not conform to the grammar.
/// This exception includes location information (line and column numbers)
/// to help identify the problematic code.
/// </summary>
public class ToonSyntaxError : ToonError
{
    public new string Message { get; }
    public int? Line { get; }
    public int? Column { get; }

    public ToonSyntaxError(string message, int? line = null, int? column = null)
        : base(FormatMessage(message, line, column))
    {
        Message = message;
        Line = line;
        Column = column;
    }

    private static string FormatMessage(string message, int? line, int? column)
    {
        string prefix = string.Empty;
        if (line.HasValue && column.HasValue)
        {
            prefix = $"(line {line.Value}, column {column.Value}) ";
        }
        else if (line.HasValue)
        {
            prefix = $"(line {line.Value}) ";
        }
        return $"{prefix}{message}";
    }
}

/// <summary>
/// Raised when attempting to parse an unsupported format (e.g., XML).
/// This exception is thrown when the input is detected to be in a format
/// that ToonSharp cannot convert.
/// </summary>
public class UnsupportedFormatException : ToonError
{
    /// <summary>
    /// The detected format name (e.g., "XML", "Unknown").
    /// </summary>
    public string DetectedFormat { get; }

    /// <summary>
    /// List of formats that are supported by ToonSharp.
    /// </summary>
    public static readonly string[] SupportedFormats = { "JSON", "YAML", "TOML", "TOON" };

    public UnsupportedFormatException(string detectedFormat)
        : base($"Unsupported format '{detectedFormat}'. Supported formats: {string.Join(", ", SupportedFormats)}")
    {
        DetectedFormat = detectedFormat;
    }

    public UnsupportedFormatException(string detectedFormat, string additionalInfo)
        : base($"Unsupported format '{detectedFormat}': {additionalInfo}. Supported formats: {string.Join(", ", SupportedFormats)}")
    {
        DetectedFormat = detectedFormat;
    }
}

/// <summary>
/// Represents a validation finding emitted by ValidateToon.
/// Includes location information and severity level for use in linting tools.
/// </summary>
public class ValidationError
{
    public string Message { get; }
    public int? Line { get; }
    public int? Column { get; }
    public string Severity { get; } // "error" or "warning"

    public ValidationError(string message, int? line = null, int? column = null, string severity = "error")
    {
        Message = message;
        Line = line;
        Column = column;
        Severity = severity;
    }

    public override string ToString()
    {
        string location = string.Empty;
        if (Line.HasValue)
        {
            location = $"line {Line.Value}";
            if (Column.HasValue)
            {
                location += $", column {Column.Value}";
            }
        }
        return $"[{Severity}] {location}: {Message}";
    }
}

