# Endpoint Filters in ASP.NET Core 7

This example shows the use of Endpoint filters to provide
cross-cutting functionality on endpoints. 

Given a response of `ILinks`, the filter will traverse the response graph and generate links for each instance of `ILinks` given the metadata found on
the response.

## Getting Started

You'll .NET 7 (preview at the time of this example)

## Endpoint Filter Code

```csharp
public class LinkGenerationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var result = await next(context);

        if (result is ILinks response)
        {
            var httpContext = context.HttpContext;
            var generator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();
            AddLinksToResponse(httpContext, generator, response);
        }

        return result;
    }

    private void AddLinksToResponse(HttpContext httpContext, LinkGenerator generator, ILinks response)
    {
        var type = response.GetType();
        var links = Attribute
            .GetCustomAttributes(response.GetType(), typeof(LinkAttribute))
            .Cast<LinkAttribute>()
            .ToList();

        foreach (var meta in links)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var parameters = properties
                .Where(p => meta.Parameters?.Contains(p.Name) == true)
                .Select(p =>
                {
                    var name = p.Name;
                    var value = p.GetValue(response);
                    return new KeyValuePair<string, object?>(name, value);
                })
                .ToList();

            var url = generator.GetUriByName(
                httpContext,
                meta.Name, 
                new RouteValueDictionary(parameters)
            );

            response.Links.Add(new Link
            {
                Name = meta.Name,
                Url = url,
                Description = meta.Description
            });

            // scan other properties
            var nested = properties.Where(p => p.PropertyType.IsAssignableTo(typeof(ILinks)) ||
                                               p.PropertyType.IsAssignableTo(typeof(IEnumerable<ILinks>)));

            foreach (var info in nested)
            {
                var value = info.GetValue(response);
                switch (value)
                {
                    case IEnumerable<ILinks> e:
                    {
                        foreach (var nestedResponse in e)
                        {
                            AddLinksToResponse(httpContext, generator, nestedResponse);
                        }

                        break;
                    }
                    case ILinks nestedResponse:
                        AddLinksToResponse(httpContext, generator, nestedResponse);
                        break;
                }
            }
        }
    }
}
```

## Endpoint Filter Usage

I created an extension method that basically calls `builder.AddEndpointFilter<LinkGenerationFilter>`.

```csharp
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
```

## Results

```json
{
  "links": [
    {
      "name": "root#index",
      "description": "All endpoints",
      "url": "/"
    },
    {
      "name": "people#index",
      "description": "List all the people",
      "url": "/people"
    }
  ]
}
```