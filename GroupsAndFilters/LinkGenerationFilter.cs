using System.Reflection;

public static class LinksGenerationFilterExtensions
{
    public static RouteGroupBuilder AddLinkGeneration(this RouteGroupBuilder builder) 
        => builder.AddEndpointFilter<LinkGenerationFilter>();

    public static RouteHandlerBuilder AddLinkGeneration(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<LinkGenerationFilter>();
} 

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
            var generator = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
            AddLinksToResponse(response, generator);
        }

        return result;
    }

    private void AddLinksToResponse(ILinks response, LinkGenerator generator)
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

            var url = generator.GetPathByName(meta.Name, new RouteValueDictionary(parameters));

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
                            AddLinksToResponse(nestedResponse, generator);
                        }

                        break;
                    }
                    case ILinks nestedResponse:
                        AddLinksToResponse(nestedResponse, generator);
                        break;
                }
            }
        }
    }
}