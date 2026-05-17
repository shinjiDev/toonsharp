using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonSharp;
using Xunit;

namespace ToonSharp.Tests;

public class SpecV3ListItemTests
{
    [Fact]
    public void Encode_list_item_with_tabular_first_field_uses_v3_canonical_form()
    {
        var data = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["users"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["id"] = 1, ["name"] = "Ada" },
                        new Dictionary<string, object?> { ["id"] = 2, ["name"] = "Bob" }
                    },
                    ["status"] = "active"
                }
            }
        };

        var toon = Api.ToToon(data, indent: 2, mode: "auto");
        Assert.Contains("- users[2]{id,name}:", toon);
        Assert.Contains("status: active", toon);
        Assert.DoesNotContain("\n  users[2]{id,name}:", toon);

        var roundTrip = Api.FromToon(toon) as Dictionary<string, object?>;
        Assert.NotNull(roundTrip);
        var items = roundTrip["items"] as List<object?>;
        Assert.NotNull(items);
        var item = items[0] as Dictionary<string, object?>;
        Assert.NotNull(item);
        Assert.Equal("active", item["status"]);
        var users = item["users"] as List<Dictionary<string, object?>>;
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public void Encode_single_field_tabular_on_hyphen_line()
    {
        var data = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["users"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["id"] = 1 },
                        new Dictionary<string, object?> { ["id"] = 2 }
                    },
                    ["note"] = "x"
                }
            }
        };

        var toon = Api.ToToon(data, indent: 2, mode: "auto");
        Assert.Contains("items[1]:", toon);
        Assert.Contains("- users[2]{id}:", toon);
        Assert.Contains("note: x", toon);
    }

    [Fact]
    public void Decode_v3_list_item_tabular_example_from_spec()
    {
        const string toon = """
items[1]:
  - users[2]{id,name}:
      1,Ada
      2,Bob
    status: active
""";

        var parsed = Api.FromToon(toon) as Dictionary<string, object?>;
        Assert.NotNull(parsed);
        var items = parsed["items"] as List<object?>;
        Assert.NotNull(items);
        var item = items[0] as Dictionary<string, object?>;
        Assert.NotNull(item);
        Assert.Equal("active", item["status"]);
        var users = item["users"] as List<Dictionary<string, object?>>;
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Equal(1L, users[0]["id"]);
        Assert.Equal("Ada", users[0]["name"]);
    }

    [Fact]
    public void Encode_empty_list_item_object_as_bare_hyphen()
    {
        var data = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                "first",
                "second",
                new Dictionary<string, object?>()
            }
        };

        var toon = Api.ToToon(data, indent: 2, mode: "auto");
        Assert.Contains("-", toon);
        Assert.Contains("- first", toon);
        Assert.Contains("- second", toon);

        var parsed = Api.FromToon(toon) as Dictionary<string, object?>;
        Assert.NotNull(parsed);
        var items = parsed["items"] as List<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.IsType<Dictionary<string, object?>>(items[2]);
        Assert.Empty((Dictionary<string, object?>)items[2]!);
    }

    [Fact]
    public void Encode_first_primitive_field_on_hyphen_line()
    {
        var data = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["name"] = "Ada", ["nums"] = new List<object?> { 1, 2, 3 } }
            }
        };

        var toon = Api.ToToon(data, indent: 2, mode: "auto");
        Assert.StartsWith("items[1]:\n  - name: Ada", toon.TrimEnd());
        Assert.Contains("nums[3]: 1,2,3", toon);

        var parsed = Api.FromToon(toon) as Dictionary<string, object?>;
        Assert.NotNull(parsed);
        var item = ((List<object?>)parsed["items"]!)[0] as Dictionary<string, object?>;
        Assert.Equal("Ada", item!["name"]);
        var nums = item["nums"] as List<object?>;
        Assert.Equal(3, nums!.Count);
    }
}
