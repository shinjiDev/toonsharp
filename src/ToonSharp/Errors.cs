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
        string prefix = "";
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
        string location = "";
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

