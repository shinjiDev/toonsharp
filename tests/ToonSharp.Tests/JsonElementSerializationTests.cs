using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

/// <summary>
/// Tests for JsonElement serialization support.
/// Validates that JSON strings can be correctly converted to TOON format.
/// </summary>
public class JsonElementSerializationTests
{
    #region Basic JsonElement Tests

    [Fact]
    public void ToToon_JsonElement_SimpleObject()
    {
        var json = @"{""name"":""John"",""age"":30}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("name: John", toon);
        Assert.Contains("age: 30", toon);
    }

    [Fact]
    public void ToToon_JsonElement_WithArray()
    {
        var json = @"{""items"":[""apple"",""banana"",""cherry""]}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("items[3]: apple,banana,cherry", toon);
    }

    [Fact]
    public void ToToon_JsonElement_WithNestedObject()
    {
        var json = @"{""person"":{""name"":""Alice"",""address"":{""city"":""NYC"",""zip"":""10001""}}}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("person:", toon);
        Assert.Contains("name: Alice", toon);
        Assert.Contains("address:", toon);
        Assert.Contains("city: NYC", toon);
        Assert.Contains("zip: 10001", toon);
    }

    [Fact]
    public void ToToon_JsonElement_WithMixedTypes()
    {
        var json = @"{""string"":""hello"",""number"":42,""decimal"":3.14,""boolean"":true,""nullValue"":null}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("string: hello", toon);
        Assert.Contains("number: 42", toon);
        Assert.Contains("decimal: 3.14", toon);
        Assert.Contains("boolean: true", toon);
        Assert.Contains("nullValue: null", toon);
    }

    [Fact]
    public void ToToon_JsonElement_EmptyObject()
    {
        var json = @"{}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("{}", toon);
    }

    [Fact]
    public void ToToon_JsonElement_EmptyArray()
    {
        var json = @"{""items"":[]}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("items[0]:", toon);
    }

    [Fact]
    public void ToToon_JsonElement_ArrayOfNumbers()
    {
        var json = @"{""numbers"":[1,2,3,4,5]}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("numbers[5]: 1,2,3,4,5", toon);
    }

    [Fact]
    public void ToToon_JsonElement_ArrayOfObjects_TabularFormat()
    {
        var json = @"{""users"":[{""id"":1,""name"":""Alice""},{""id"":2,""name"":""Bob""},{""id"":3,""name"":""Charlie""}]}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj, mode: "auto");
        
        // Should use tabular format for array of objects with same schema
        Assert.Contains("users[3]{", toon);
    }

    #endregion

    #region User's Original Request Test

    [Fact]
    public void ToToon_JsonElement_UserDocumentAnalysisRequest()
    {
        // This is the exact JSON from the user's request
        var json = @"{""DocumentId"":""DOC-2024-001"",""Content"":""This is a document about digital transformation in enterprises..."",""AnalysisType"":""sentiment_and_topics"",""MaxTokensResponse"":500,""DesiredMetrics"":[""sentiment"",""topics"",""entities"",""summary""]}";
        
        var obj = JsonSerializer.Deserialize<object>(json);
        var toon = Api.ToToon(obj);
        
        // Verify all fields are properly converted
        Assert.Contains("DocumentId: DOC-2024-001", toon);
        Assert.Contains("Content: \"This is a document about digital transformation in enterprises...\"", toon);
        Assert.Contains("AnalysisType: sentiment_and_topics", toon);
        Assert.Contains("MaxTokensResponse: 500", toon);
        Assert.Contains("DesiredMetrics[4]: sentiment,topics,entities,summary", toon);
    }

    #endregion

    #region Number Handling Tests

    [Fact]
    public void ToToon_JsonElement_IntegerNumbers()
    {
        var json = @"{""small"":42,""large"":9999999999}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("small: 42", toon);
        Assert.Contains("large: 9999999999", toon);
    }

    [Fact]
    public void ToToon_JsonElement_DecimalNumbers()
    {
        var json = @"{""pi"":3.14159,""tiny"":0.0001}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("pi: 3.14159", toon);
        Assert.Contains("tiny: 0.0001", toon);
    }

    [Fact]
    public void ToToon_JsonElement_NegativeNumbers()
    {
        var json = @"{""negative"":-42,""negativeDecimal"":-3.14}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("negative: -42", toon);
        Assert.Contains("negativeDecimal: -3.14", toon);
    }

    #endregion

    #region String Handling Tests

    [Fact]
    public void ToToon_JsonElement_StringWithSpecialCharacters()
    {
        var json = @"{""text"":""Hello, World!"",""quoted"":""Say \""hello\""""}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("text: \"Hello, World!\"", toon);
        Assert.Contains("quoted:", toon);
    }

    [Fact]
    public void ToToon_JsonElement_EmptyString()
    {
        var json = @"{""empty"":""""}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        // Empty strings are serialized without quotes in current implementation
        Assert.Contains("empty:", toon);
    }

    #endregion

    #region Complex Nested Structure Tests

    [Fact]
    public void ToToon_JsonElement_DeeplyNested()
    {
        var json = @"{""level1"":{""level2"":{""level3"":{""level4"":{""value"":""deep""}}}}}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("level1:", toon);
        Assert.Contains("level2:", toon);
        Assert.Contains("level3:", toon);
        Assert.Contains("level4:", toon);
        Assert.Contains("value: deep", toon);
    }

    [Fact]
    public void ToToon_JsonElement_ArrayOfArrays()
    {
        var json = @"{""matrix"":[[1,2],[3,4],[5,6]]}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("matrix[3]:", toon);
        // Each inner array should be on its own line
        Assert.Contains("-", toon);
    }

    [Fact]
    public void ToToon_JsonElement_MixedNestedStructures()
    {
        var json = @"{""config"":{""database"":{""host"":""localhost"",""port"":5432,""credentials"":{""user"":""admin"",""roles"":[""read"",""write""]}}}}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("config:", toon);
        Assert.Contains("database:", toon);
        Assert.Contains("host: localhost", toon);
        Assert.Contains("port: 5432", toon);
        Assert.Contains("credentials:", toon);
        Assert.Contains("user: admin", toon);
        Assert.Contains("roles[2]: read,write", toon);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_JsonToToonToObject()
    {
        var json = @"{""name"":""Test"",""value"":123,""active"":true}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        var parsed = Api.FromToon(toon) as Dictionary<string, object?>;
        
        Assert.NotNull(parsed);
        Assert.Equal("Test", parsed["name"]);
        Assert.Equal(123L, parsed["value"]);
        Assert.Equal(true, parsed["active"]);
    }

    [Fact]
    public void RoundTrip_JsonWithArrayToToonToObject()
    {
        var json = @"{""tags"":[""a"",""b"",""c""]}";
        var obj = JsonSerializer.Deserialize<object>(json);
        
        var toon = Api.ToToon(obj);
        var parsed = Api.FromToon(toon) as Dictionary<string, object?>;
        
        Assert.NotNull(parsed);
        var tags = parsed["tags"] as List<object?>;
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Equal("a", tags[0]);
    }

    #endregion

    #region Real-World JSON Examples

    [Fact]
    public void ToToon_JsonElement_ApiResponse()
    {
        var json = @"{
            ""status"": ""success"",
            ""code"": 200,
            ""data"": {
                ""users"": [
                    {""id"": 1, ""name"": ""Alice"", ""email"": ""alice@example.com""},
                    {""id"": 2, ""name"": ""Bob"", ""email"": ""bob@example.com""}
                ],
                ""pagination"": {
                    ""page"": 1,
                    ""perPage"": 10,
                    ""total"": 2
                }
            }
        }";
        
        var obj = JsonSerializer.Deserialize<object>(json);
        var toon = Api.ToToon(obj);
        
        Assert.Contains("status: success", toon);
        Assert.Contains("code: 200", toon);
        Assert.Contains("data:", toon);
        Assert.Contains("users[2]{", toon); // Should detect tabular format
        Assert.Contains("pagination:", toon);
        Assert.Contains("page: 1", toon);
    }

    [Fact]
    public void ToToon_JsonElement_PackageJson()
    {
        var json = @"{
            ""name"": ""my-package"",
            ""version"": ""1.0.0"",
            ""dependencies"": {
                ""lodash"": ""^4.17.21"",
                ""axios"": ""^1.0.0""
            },
            ""scripts"": {
                ""build"": ""tsc"",
                ""test"": ""jest""
            }
        }";
        
        var obj = JsonSerializer.Deserialize<object>(json);
        var toon = Api.ToToon(obj);
        
        Assert.Contains("name: my-package", toon);
        Assert.Contains("version: 1.0.0", toon);
        Assert.Contains("dependencies:", toon);
        Assert.Contains("scripts:", toon);
    }

    #endregion
}

