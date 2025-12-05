using System.Collections.Generic;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

/// <summary>
/// Tests for POCO (Plain Old CLR Object) serialization support.
/// Validates that .NET classes can be correctly converted to TOON format.
/// </summary>
public class PocoSerializationTests
{
    #region Test Classes

    public class SimplePerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    public class PersonWithAddress
    {
        public string Name { get; set; } = "";
        public Address HomeAddress { get; set; } = new();
    }

    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string ZipCode { get; set; } = "";
    }

    public class PersonWithList
    {
        public string Name { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public string[] Skills { get; set; } = [];
    }

    public class PersonWithNestedList
    {
        public string Name { get; set; } = "";
        public List<Address> Addresses { get; set; } = new();
    }

    public class ComplexEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public bool InStock { get; set; }
        public List<string> Categories { get; set; } = new();
        public Dictionary<string, object?> Metadata { get; set; } = new();
    }

    public class EntityWithNullables
    {
        public string? NullableString { get; set; }
        public int? NullableInt { get; set; }
        public bool? NullableBool { get; set; }
    }

    public record PersonRecord(string Name, int Age);

    public class ClassWithFields
    {
        public string PublicField = "public";
        private string PrivateField = "private";
        public string GetPrivateField() => PrivateField;
    }

    #endregion

    #region Simple POCO Tests

    [Fact]
    public void ToToon_SimplePoco_SerializesCorrectly()
    {
        var person = new SimplePerson
        {
            Name = "John Doe",
            Age = 30,
            IsActive = true
        };
        
        var toon = Api.ToToon(person);
        
        // Strings with spaces are quoted in TOON format
        Assert.Contains("Name: \"John Doe\"", toon);
        Assert.Contains("Age: 30", toon);
        Assert.Contains("IsActive: true", toon);
    }

    [Fact]
    public void ToToon_PocoWithNestedObject_SerializesCorrectly()
    {
        var person = new PersonWithAddress
        {
            Name = "Alice",
            HomeAddress = new Address
            {
                Street = "123 Main St",
                City = "New York",
                ZipCode = "10001"
            }
        };
        
        var toon = Api.ToToon(person);
        
        Assert.Contains("Name: Alice", toon);
        Assert.Contains("HomeAddress:", toon);
        // Strings with spaces are quoted in TOON format
        Assert.Contains("Street: \"123 Main St\"", toon);
        Assert.Contains("City: \"New York\"", toon);
        Assert.Contains("ZipCode: 10001", toon);
    }

    [Fact]
    public void ToToon_PocoWithList_SerializesCorrectly()
    {
        var person = new PersonWithList
        {
            Name = "Bob",
            Tags = new List<string> { "developer", "blogger" },
            Skills = new[] { "C#", "Python", "JavaScript" }
        };
        
        var toon = Api.ToToon(person);
        
        Assert.Contains("Name: Bob", toon);
        Assert.Contains("Tags:", toon);
        Assert.Contains("- developer", toon);
        Assert.Contains("- blogger", toon);
        Assert.Contains("Skills:", toon);
        Assert.Contains("- C#", toon);
        Assert.Contains("- Python", toon);
        Assert.Contains("- JavaScript", toon);
    }

    [Fact]
    public void ToToon_PocoWithNestedList_SerializesCorrectly()
    {
        var person = new PersonWithNestedList
        {
            Name = "Charlie",
            Addresses = new List<Address>
            {
                new Address { Street = "Home St", City = "Boston", ZipCode = "02101" },
                new Address { Street = "Work Ave", City = "Cambridge", ZipCode = "02139" }
            }
        };
        
        var toon = Api.ToToon(person);
        
        Assert.Contains("Name: Charlie", toon);
        Assert.Contains("Addresses[2]{", toon); // Should use tabular format
    }

    #endregion

    #region Complex POCO Tests

    [Fact]
    public void ToToon_ComplexEntity_SerializesCorrectly()
    {
        var entity = new ComplexEntity
        {
            Id = 42,
            Title = "Amazing Product",
            Price = 99.99m,
            InStock = true,
            Categories = new List<string> { "Electronics", "Gadgets" },
            Metadata = new Dictionary<string, object?>
            {
                { "manufacturer", "TechCorp" },
                { "warranty_years", 2 }
            }
        };
        
        var toon = Api.ToToon(entity);
        
        Assert.Contains("Id: 42", toon);
        // Strings with spaces are quoted in TOON format
        Assert.Contains("Title: \"Amazing Product\"", toon);
        Assert.Contains("Price: 99.99", toon);
        Assert.Contains("InStock: true", toon);
        Assert.Contains("Categories:", toon);
        Assert.Contains("- Electronics", toon);
        Assert.Contains("Metadata:", toon);
        Assert.Contains("manufacturer: TechCorp", toon);
    }

    [Fact]
    public void ToToon_EntityWithNullables_HandlesNullsCorrectly()
    {
        var entity = new EntityWithNullables
        {
            NullableString = null,
            NullableInt = null,
            NullableBool = true
        };
        
        var toon = Api.ToToon(entity);
        
        Assert.Contains("NullableString: null", toon);
        Assert.Contains("NullableInt: null", toon);
        Assert.Contains("NullableBool: true", toon);
    }

    [Fact]
    public void ToToon_EntityWithNullables_HandlesValuesCorrectly()
    {
        var entity = new EntityWithNullables
        {
            NullableString = "hello",
            NullableInt = 42,
            NullableBool = false
        };
        
        var toon = Api.ToToon(entity);
        
        Assert.Contains("NullableString: hello", toon);
        Assert.Contains("NullableInt: 42", toon);
        Assert.Contains("NullableBool: false", toon);
    }

    #endregion

    #region Record Tests

    [Fact]
    public void ToToon_Record_SerializesCorrectly()
    {
        var person = new PersonRecord("Jane", 25);
        
        var toon = Api.ToToon(person);
        
        Assert.Contains("Name: Jane", toon);
        Assert.Contains("Age: 25", toon);
    }

    #endregion

    #region Anonymous Type Tests

    [Fact]
    public void ToToon_AnonymousType_SerializesCorrectly()
    {
        var anon = new { Name = "Anonymous", Value = 123 };
        
        var toon = Api.ToToon(anon);
        
        Assert.Contains("Name: Anonymous", toon);
        Assert.Contains("Value: 123", toon);
    }

    [Fact]
    public void ToToon_AnonymousTypeWithArray_SerializesCorrectly()
    {
        var anon = new { Items = new[] { "a", "b", "c" } };
        
        var toon = Api.ToToon(anon);
        
        Assert.Contains("Items:", toon);
        Assert.Contains("- a", toon);
        Assert.Contains("- b", toon);
        Assert.Contains("- c", toon);
    }

    [Fact]
    public void ToToon_NestedAnonymousTypes_SerializesCorrectly()
    {
        var anon = new
        {
            Person = new { Name = "John", Age = 30 },
            Settings = new { Theme = "dark", Language = "en" }
        };
        
        var toon = Api.ToToon(anon);
        
        Assert.Contains("Person:", toon);
        Assert.Contains("Name: John", toon);
        Assert.Contains("Settings:", toon);
        Assert.Contains("Theme: dark", toon);
    }

    #endregion

    #region Field Tests

    [Fact]
    public void ToToon_ClassWithPublicFields_IncludesFields()
    {
        var obj = new ClassWithFields();
        
        var toon = Api.ToToon(obj);
        
        Assert.Contains("PublicField: public", toon);
        // Private fields should not be included
        Assert.DoesNotContain("PrivateField", toon);
    }

    #endregion

    #region Empty/Edge Cases

    [Fact]
    public void ToToon_EmptyPoco_SerializesAsEmptyObject()
    {
        var empty = new SimplePerson();
        
        var toon = Api.ToToon(empty);
        
        // Should contain default values
        Assert.Contains("Name:", toon);
        Assert.Contains("Age: 0", toon);
        Assert.Contains("IsActive: false", toon);
    }

    [Fact]
    public void ToToon_EmptyList_SerializesCorrectly()
    {
        var person = new PersonWithList
        {
            Name = "Empty",
            Tags = new List<string>(),
            Skills = []
        };
        
        var toon = Api.ToToon(person);
        
        Assert.Contains("Name: Empty", toon);
        Assert.Contains("Tags: []", toon);
        Assert.Contains("Skills: []", toon);
    }

    #endregion

    #region List of POCOs Tests

    [Fact]
    public void ToToon_ListOfPocos_UsesTabularFormat()
    {
        var people = new List<SimplePerson>
        {
            new SimplePerson { Name = "Alice", Age = 25, IsActive = true },
            new SimplePerson { Name = "Bob", Age = 30, IsActive = false },
            new SimplePerson { Name = "Charlie", Age = 35, IsActive = true }
        };
        
        var wrapper = new { People = people };
        var toon = Api.ToToon(wrapper);
        
        // Should use tabular format
        Assert.Contains("People[3]{", toon);
    }

    [Fact]
    public void ToToon_ArrayOfPocos_SerializesCorrectly()
    {
        var addresses = new Address[]
        {
            new Address { Street = "First St", City = "City1", ZipCode = "11111" },
            new Address { Street = "Second St", City = "City2", ZipCode = "22222" }
        };
        
        var wrapper = new { Locations = addresses };
        var toon = Api.ToToon(wrapper);
        
        Assert.Contains("Locations[2]{", toon);
    }

    [Fact]
    public void ToToon_RootListOfPocos_SerializesAsTabular()
    {
        // Test that a root-level list of POCOs serializes in tabular format
        var requests = new List<SimplePerson>
        {
            new SimplePerson { Name = "Alice", Age = 30, IsActive = true },
            new SimplePerson { Name = "Bob", Age = 25, IsActive = false },
            new SimplePerson { Name = "Charlie", Age = 35, IsActive = true }
        };
        
        var toon = Api.ToToon(requests);
        
        // Should use tabular format: [3]{Name,Age,IsActive}:
        Assert.Contains("[3]{", toon);
        Assert.Contains("Name", toon);
        Assert.Contains("Age", toon);
        Assert.Contains("IsActive", toon);
        Assert.Contains("Alice", toon);
        Assert.Contains("Bob", toon);
        Assert.Contains("Charlie", toon);
        // Should NOT have "-" list markers (those indicate non-tabular format)
        Assert.DoesNotContain("- ", toon);
    }

    [Fact]
    public void ToToon_RootArrayOfPocos_SerializesAsTabular()
    {
        // Test with array instead of List
        var addresses = new Address[]
        {
            new Address { Street = "First St", City = "City1", ZipCode = "11111" },
            new Address { Street = "Second St", City = "City2", ZipCode = "22222" }
        };
        
        var toon = Api.ToToon(addresses);
        
        // Should use tabular format: [2]{Street,City,ZipCode}:
        Assert.Contains("[2]{", toon);
        Assert.Contains("Street", toon);
        Assert.Contains("City", toon);
        Assert.Contains("ZipCode", toon);
        Assert.DoesNotContain("- ", toon);
    }

    #endregion
}

