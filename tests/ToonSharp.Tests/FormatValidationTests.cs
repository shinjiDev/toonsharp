using System;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

/// <summary>
/// Tests for format validation and unsupported format detection.
/// Ensures that ToonSharp correctly identifies and rejects unsupported formats like XML.
/// </summary>
public class FormatValidationTests
{
    #region Format Detection Tests

    [Fact]
    public void DetectFormat_ValidJson_ReturnsJson()
    {
        var json = @"{""name"":""test"",""value"":123}";
        Assert.Equal("JSON", Api.DetectFormat(json));
    }

    [Fact]
    public void DetectFormat_JsonArray_ReturnsJson()
    {
        var json = @"[1, 2, 3]";
        Assert.Equal("JSON", Api.DetectFormat(json));
    }

    [Fact]
    public void DetectFormat_JsonWithWhitespace_ReturnsJson()
    {
        var json = @"   
        {""name"":""test""}";
        Assert.Equal("JSON", Api.DetectFormat(json));
    }

    [Fact]
    public void DetectFormat_XmlDeclaration_ReturnsXml()
    {
        var xml = @"<?xml version=""1.0""?><root><item>test</item></root>";
        Assert.Equal("XML", Api.DetectFormat(xml));
    }

    [Fact]
    public void DetectFormat_XmlElement_ReturnsXml()
    {
        var xml = @"<root><item>test</item></root>";
        Assert.Equal("XML", Api.DetectFormat(xml));
    }

    [Fact]
    public void DetectFormat_XmlWithNamespace_ReturnsXml()
    {
        var xml = @"<ns:root xmlns:ns=""http://example.com""><ns:item>test</ns:item></ns:root>";
        Assert.Equal("XML", Api.DetectFormat(xml));
    }

    [Fact]
    public void DetectFormat_HtmlDoctype_ReturnsHtml()
    {
        var html = @"<!DOCTYPE html><html></html>";
        Assert.Equal("HTML", Api.DetectFormat(html));
    }

    [Fact]
    public void DetectFormat_XmlDoctype_ReturnsXml()
    {
        var xml = @"<!DOCTYPE note SYSTEM ""note.dtd""><note><to>User</to></note>";
        Assert.Equal("XML", Api.DetectFormat(xml));
    }

    [Fact]
    public void DetectFormat_Html_ReturnsHtml()
    {
        var html = @"<html><body><p>Hello</p></body></html>";
        Assert.Equal("HTML", Api.DetectFormat(html));
    }

    [Fact]
    public void DetectFormat_EmptyString_ReturnsUnknown()
    {
        Assert.Equal("Unknown", Api.DetectFormat(""));
        Assert.Equal("Unknown", Api.DetectFormat("   "));
    }

    [Fact]
    public void DetectFormat_PlainText_ReturnsUnknown()
    {
        var text = "This is just plain text without any format markers.";
        Assert.Equal("Unknown", Api.DetectFormat(text));
    }

    [Fact]
    public void DetectFormat_Csv_ReturnsCsv()
    {
        var csv = "name,age,city\nJohn,30,NYC\nJane,25,LA";
        Assert.Equal("CSV", Api.DetectFormat(csv));
    }

    #endregion

    #region JsonToToon Validation Tests

    [Fact]
    public void JsonToToon_ValidJson_Succeeds()
    {
        var json = @"{""name"":""test"",""value"":123}";
        var toon = Api.JsonToToon(json);
        
        Assert.Contains("name: test", toon);
        Assert.Contains("value: 123", toon);
    }

    [Fact]
    public void JsonToToon_XmlInput_ThrowsUnsupportedFormatException()
    {
        var xml = @"<?xml version=""1.0""?><root><item>test</item></root>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(xml));
        Assert.Equal("XML", ex.DetectedFormat);
        Assert.Contains("Supported formats:", ex.Message);
    }

    [Fact]
    public void JsonToToon_XmlElement_ThrowsUnsupportedFormatException()
    {
        var xml = @"<configuration><setting name=""test"" value=""123""/></configuration>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(xml));
        Assert.Equal("XML", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_HtmlInput_ThrowsUnsupportedFormatException()
    {
        var html = @"<html><head><title>Test</title></head><body></body></html>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(html));
        Assert.Equal("HTML", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_CsvInput_ThrowsUnsupportedFormatException()
    {
        var csv = "id,name,value\n1,test,100\n2,sample,200";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(csv));
        Assert.Equal("CSV", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_NullInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Api.JsonToToon(null!));
    }

    [Fact]
    public void JsonToToon_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Api.JsonToToon(""));
        Assert.Throws<ArgumentException>(() => Api.JsonToToon("   "));
    }

    [Fact]
    public void JsonToToon_InvalidJson_ThrowsUnsupportedFormatException()
    {
        var invalidJson = @"{name: test, value: 123}"; // Missing quotes
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(invalidJson));
        Assert.Contains("Invalid JSON", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_MalformedJson_ThrowsUnsupportedFormatException()
    {
        var malformed = @"{""name"":""test"", ""value"":}"; // Missing value
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(malformed));
        Assert.Contains("Invalid JSON", ex.DetectedFormat);
    }

    #endregion

    #region FromJson Validation Tests

    [Fact]
    public void FromJson_ValidJson_ReturnsObject()
    {
        var json = @"{""name"":""test""}";
        var result = Api.FromJson(json);
        
        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_XmlInput_ThrowsUnsupportedFormatException()
    {
        var xml = @"<root><item>test</item></root>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.FromJson(xml));
        Assert.Equal("XML", ex.DetectedFormat);
    }

    [Fact]
    public void FromJson_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Api.FromJson(""));
    }

    #endregion

    #region IsFormatSupported Tests

    [Theory]
    [InlineData("JSON", true)]
    [InlineData("json", true)]
    [InlineData("YAML", true)]
    [InlineData("yaml", true)]
    [InlineData("TOML", true)]
    [InlineData("toml", true)]
    [InlineData("TOON", true)]
    [InlineData("toon", true)]
    [InlineData("XML", false)]
    [InlineData("xml", false)]
    [InlineData("HTML", false)]
    [InlineData("CSV", false)]
    [InlineData("INI", false)]
    [InlineData("Unknown", false)]
    public void IsFormatSupported_ReturnsCorrectResult(string format, bool expected)
    {
        Assert.Equal(expected, Api.IsFormatSupported(format));
    }

    #endregion

    #region Real-World XML Examples

    [Fact]
    public void JsonToToon_SoapEnvelope_ThrowsUnsupportedFormatException()
    {
        var soap = @"<?xml version=""1.0""?>
<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
    <soap:Body>
        <GetUserRequest>
            <UserId>123</UserId>
        </GetUserRequest>
    </soap:Body>
</soap:Envelope>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(soap));
        Assert.Equal("XML", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_RssXml_ThrowsUnsupportedFormatException()
    {
        var rss = @"<?xml version=""1.0""?>
<rss version=""2.0"">
    <channel>
        <title>My Feed</title>
        <item><title>Article 1</title></item>
    </channel>
</rss>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(rss));
        Assert.Equal("XML", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_CsProjXml_ThrowsUnsupportedFormatException()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
    </PropertyGroup>
</Project>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(csproj));
        Assert.Equal("XML", ex.DetectedFormat);
    }

    [Fact]
    public void JsonToToon_AndroidLayoutXml_ThrowsUnsupportedFormatException()
    {
        var layout = @"<?xml version=""1.0"" encoding=""utf-8""?>
<LinearLayout xmlns:android=""http://schemas.android.com/apk/res/android""
    android:layout_width=""match_parent""
    android:layout_height=""match_parent"">
    <TextView android:text=""Hello""/>
</LinearLayout>";
        
        var ex = Assert.Throws<UnsupportedFormatException>(() => Api.JsonToToon(layout));
        Assert.Equal("XML", ex.DetectedFormat);
    }

    #endregion

    #region Exception Properties Tests

    [Fact]
    public void UnsupportedFormatException_ContainsSupportedFormats()
    {
        var ex = new UnsupportedFormatException("XML");
        
        Assert.Contains("JSON", ex.Message);
        Assert.Contains("YAML", ex.Message);
        Assert.Contains("TOML", ex.Message);
        Assert.Contains("TOON", ex.Message);
    }

    [Fact]
    public void UnsupportedFormatException_SupportedFormatsArray_IsCorrect()
    {
        Assert.Contains("JSON", UnsupportedFormatException.SupportedFormats);
        Assert.Contains("YAML", UnsupportedFormatException.SupportedFormats);
        Assert.Contains("TOML", UnsupportedFormatException.SupportedFormats);
        Assert.Contains("TOON", UnsupportedFormatException.SupportedFormats);
        Assert.Equal(4, UnsupportedFormatException.SupportedFormats.Length);
    }

    #endregion
}

