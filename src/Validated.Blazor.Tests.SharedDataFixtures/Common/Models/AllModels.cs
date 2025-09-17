namespace Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
public record class ContactMethodDto(string MethodType, string MethodValue);
public record class AddressDto
{
    public string AddressLine { get; set; } = default!;
    public string TownCity    { get; set; } = default!;
    public string County      { get; set; } = default!;
    public string? Postcode   { get; set; }
}
public record class ContactDto
{
    public string   Title       { get; set; } = default!;
    public string  GivenName    { get; set; } = default!;
    public string   FamilyName  { get; set; } = default!;
    public DateOnly DOB         { get; set; } = default!;
    public DateOnly CompareDOB  { get; set; } = default!;
    public string   Email       { get; set; } = default!;
    public string?  Mobile      { get; set; }
    public int?     NullableAge { get; set; }
    public int      Age         { get; set; }


    public List<string> Entries { get; set; } = [];

    public AddressDto Address          { get; set; } = default!;
    public AddressDto? NullableAddress { get; set; }

    public List<ContactMethodDto> ContactMethods { get; set; } = [];
}

public class Parent
{
    public string ParentName { get; set; }
    public Child Child       { get; set; }

    public Parent(string parentName, string childName)
    {
        ParentName = parentName;
        Child = new Child(childName, this);
    }

}   
public record Child(string ChildName, Parent Parent);

public record PrimitiveCollectionHolder(string Name, List<int> Numbers);
public class ObjectWithIndexer
{
    private readonly Dictionary<string, object> _items = [];

    public object? this[string key]
    {
        get => _items.TryGetValue(key, out object? value) ? value : null;
        set => _items[key] = value!;
    }

    // Regular property that can contain nested objects
    public SimpleChild RegularProperty { get; set; } = default!;
}

public class SimpleChild
{
    public string Name { get; set; } = default!;
}
