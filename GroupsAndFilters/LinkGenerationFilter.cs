using System.Reflection;

namespace GroupsAndFilters;

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