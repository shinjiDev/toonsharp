using System;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class ErrorsTests
{
    [Fact]
    public void ToonError_Message()
    {
        var error = new ToonError("Test error");
        Assert.Equal("Test error", error.Message);
    }

    [Fact]
    public void ToonSyntaxError_WithLineAndColumn()
    {
        var error = new ToonSyntaxError("Syntax error", 5, 10);
        Assert.Equal("Syntax error", error.Message);
        Assert.Equal(5, error.Line);
        Assert.Equal(10, error.Column);
        // The base Exception.Message includes the formatted message with line/column
        // Access via base class to get the formatted message
        var formattedMessage = ((Exception)error).Message;
        Assert.Contains("line 5, column 10", formattedMessage);
    }

    [Fact]
    public void ToonSyntaxError_WithLineOnly()
    {
        var error = new ToonSyntaxError("Syntax error", 5);
        Assert.Equal("Syntax error", error.Message);
        Assert.Equal(5, error.Line);
        Assert.Null(error.Column);
        // The base Exception.Message includes the formatted message with line
        // Access via base class to get the formatted message
        var formattedMessage = ((Exception)error).Message;
        Assert.Contains("line 5", formattedMessage);
    }

    [Fact]
    public void ValidationError_ToString()
    {
        var error = new ValidationError("Validation failed", 3, 5, "error");
        var str = error.ToString();
        Assert.Contains("error", str);
        Assert.Contains("line 3", str);
        Assert.Contains("column 5", str);
        Assert.Contains("Validation failed", str);
    }

    [Fact]
    public void ValidationError_Warning()
    {
        var error = new ValidationError("Warning message", 1, null, "warning");
        Assert.Equal("warning", error.Severity);
    }
}

