using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Lost_Item.Filters;

/// <summary>
/// Action filter that trims leading/trailing whitespace from all string inputs before
/// they reach controller action parameters. Handles both direct string parameters
/// (e.g. [FromForm] string brand) and string properties on bound model objects.
/// </summary>
public class TrimStringInputFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var key in context.ActionArguments.Keys.ToList())
        {
            var value = context.ActionArguments[key];
            if (value is string str)
            {
                context.ActionArguments[key] = str.Trim();
            }
            else if (value != null && IsUserDefinedClass(value.GetType()))
            {
                TrimStringProperties(value);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    // Trim all public writable string properties on a model object
    private static void TrimStringProperties(object model)
    {
        var props = model.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var prop in props)
        {
            var current = (string?)prop.GetValue(model);
            if (current != null)
                prop.SetValue(model, current.Trim());
        }
    }

    // Avoid reflecting over primitives, collections, framework types, etc.
    private static bool IsUserDefinedClass(Type type) =>
        type.IsClass && !type.IsPrimitive && type.Namespace?.StartsWith("System") == false;
}
