using System;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class UtilsTests
{
    [Fact]
    public void IsSafeIdentifier_Valid()
    {
        Assert.True(Utils.IsSafeIdentifier("name"));
        Assert.True(Utils.IsSafeIdentifier("my_key"));
        Assert.True(Utils.IsSafeIdentifier("_private"));
        Assert.True(Utils.IsSafeIdentifier("key123"));
    }

    [Fact]
    public void IsSafeIdentifier_Invalid()
    {
        Assert.False(Utils.IsSafeIdentifier("123abc"));
        Assert.False(Utils.IsSafeIdentifier("my key"));
        Assert.False(Utils.IsSafeIdentifier("key:value"));
    }

    [Fact]
    public void FormatKey_SafeIdentifier()
    {
        Assert.Equal("name", Utils.FormatKey("name"));
    }

    [Fact]
    public void FormatKey_NeedsQuotes()
    {
        var result = Utils.FormatKey("my key");
        Assert.StartsWith("\"", result);
        Assert.EndsWith("\"", result);
    }

    [Fact]
    public void FormatScalar_Null()
    {
        Assert.Equal("null", Utils.FormatScalar(null));
    }

    [Fact]
    public void FormatScalar_Boolean()
    {
        Assert.Equal("true", Utils.FormatScalar(true));
        Assert.Equal("false", Utils.FormatScalar(false));
    }

    [Fact]
    public void FormatScalar_Number()
    {
        Assert.Equal("42", Utils.FormatScalar(42));
        Assert.Equal("3.14", Utils.FormatScalar(3.14));
    }

    [Fact]
    public void FormatScalar_String()
    {
        Assert.Equal("hello", Utils.FormatScalar("hello"));
        var quoted = Utils.FormatScalar("hello world");
        Assert.StartsWith("\"", quoted);
    }

    [Fact]
    public void GuessNumber_Integer()
    {
        Assert.Equal(42L, Utils.GuessNumber("42"));
        Assert.Equal(-10L, Utils.GuessNumber("-10"));
    }

    [Fact]
    public void GuessNumber_Float()
    {
        Assert.Equal(3.14, Utils.GuessNumber("3.14"));
        Assert.Equal(1e5, Utils.GuessNumber("1e5"));
    }

    [Fact]
    public void GuessNumber_Invalid()
    {
        Assert.Null(Utils.GuessNumber("not_a_number"));
        Assert.Null(Utils.GuessNumber("abc123"));
    }
}

