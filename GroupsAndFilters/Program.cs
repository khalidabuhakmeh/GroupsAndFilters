using GroupsAndFilters;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => new RootResponse())
    .WithName("root#index")
    .AddLinkGeneration();

var people = app.MapGroup("/people")
    .AddLinkGeneration();

people.MapGet("", () => new PeopleResponse
    {
        Results = new()
        {
            new()
            {
                Id = 1,
                Name = "Khalid Abuhakmeh"
            }
        }
    }).WithName("people#index");

people.MapGet("/{id}", (int id) => new PersonResponse
    {
        Id = id,
        Name = "Khalid Abuhakmeh"
    }).WithName("people#show");

app.Run();

[Link("root#index", "All endpoints")]
[Link("people#index", "List all the people")]
public class RootResponse : ILinks
{
    public List<Link> Links { get; set; } = new();
}

[Link("people#index", "List all the people")]
public class PeopleResponse : ILinks
{
    public List<PersonResponse> Results { get; set; } = new();
    public List<Link> Links { get; set; } = new();
}

[Link("people#index", "List all the people")]
[Link("people#show", "Return one person", Parameters = new[] { "Id" })]
public class PersonResponse : ILinks
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Link> Links { get; set; } = new();
}

public interface ILinks
{
    List<Link> Links { get; set; }
}
public class Link
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Url { get; set; } = "";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class LinkAttribute : Attribute
{
    public LinkAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; init; }
    public string Description { get; set; }
    public string[]? Parameters { get; set; }
}