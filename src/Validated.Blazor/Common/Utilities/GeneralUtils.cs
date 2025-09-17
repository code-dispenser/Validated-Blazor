using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Validated.Blazor.Common.Utilities;


/// <summary>
/// Provides general-purpose utility methods for the Blazor validation components.
/// </summary>
/// <remarks>
/// This class contains helper methods for tasks such as extracting member names from expressions
/// and traversing complex object models to find property names, which is crucial for
/// mapping validation messages to the correct fields in Blazor's EditContext.
/// </remarks>
internal static class GeneralUtils
{

    private static readonly NullabilityInfoContext _context = new();

    /// <summary>
    /// Gets the member name represented by a lambda expression.
    /// </summary>
    /// <typeparam name="TEntity">The type containing the member.</typeparam>
    /// <typeparam name="TMember">The type of the member.</typeparam>
    /// <param name="selectorExpression">The member access expression.</param>
    /// <returns>The name of the member accessed in the expression.</returns>
    public static string GetMemberName<TEntity, TMember>(Expression<Func<TEntity, TMember>> selectorExpression)

        => ExtractMemberName(selectorExpression.Body) ?? String.Concat(typeof(TEntity).Name, ".", typeof(TMember).Name);

    /// <summary>
    /// Builds a unique key for a member validator, typically in the format "Parent.PropertyName".
    /// </summary>
    /// <param name="parent">The name of the parent object or model.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A concatenated key used for storing and retrieving validators in a dictionary.</returns>
    public static string BuildMemberValidatorKey(string parent, string propertyName)

        => String.IsNullOrWhiteSpace(parent) ? propertyName : String.Concat(parent, ".", propertyName);

    /// <summary>
    /// Extracts the member name from a lambda expression.
    /// </summary>
    /// <param name="expression">The member access expression.</param>
    /// <returns>The name of the accessed member.</returns>
    public static string? ExtractMemberName(Expression expression)

        => expression switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
            MethodCallExpression method when method.Method.Name == "get_Item" => "Item", // For indexers
            ParameterExpression param => param.Name, // For root parameter
            _ => null
        };


    /// <summary>
    /// Traverses a model graph to find the property name of a target model object relative to a starting model.
    /// </summary>
    /// <param name="currentModel">The model object to start the search from (the root or a nested object).</param>
    /// <param name="targetModel">The model object whose property name we are trying to find.</param>
    /// <param name="visited">A set of objects that have already been visited to prevent infinite recursion in circular object graphs.</param>
    /// <returns>The name of the property on <paramref name="currentModel"/> (or a sub-object) that holds the <paramref name="targetModel"/> instance.</returns>
    /// <remarks>
    /// This method is crucial for handling validation in nested objects within a Blazor EditContext. When a field changes
    /// in a nested component, the `FieldIdentifier.Model` will be the nested object, not the root object. This utility
    /// allows the validator to find the "path" to that nested object (e.g., "Address") from the root model to construct
    /// the correct validator key (e.g., "Address.Street").
    /// </remarks>
    public static string FindModelPropertyName(object currentModel, object targetModel, HashSet<object> visited)
    {
        if (currentModel == null || !visited.Add(currentModel)) return String.Empty;

        var properties = currentModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length > 0) continue;

            var propertyValue = property.GetValue(currentModel);
            if (propertyValue == null) continue;

            if (ReferenceEquals(propertyValue, targetModel)) return property.Name;

            var propertyType = property.PropertyType;

            if (propertyType.IsClass && propertyType != typeof(string))
            {
                if (propertyType.GetInterface(nameof(IEnumerable)) != null && propertyValue is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {

                        if (item != null) //changed from one liner to make it a bit easier to read
                        {
                            if (ReferenceEquals(item, targetModel)) return property.Name;

                            if(false == String.IsNullOrEmpty(FindModelPropertyName(item, targetModel, visited))) return property.Name;

                        }
                                
                    }
                }
                else
                {
                    var result = FindModelPropertyName(propertyValue, targetModel, visited);

                    if (!string.IsNullOrEmpty(result)) return property.Name;
                }
            }
        }

        return String.Empty;
    }

    /// <summary>
    /// Determines if a property is nullable using reflection.
    /// </summary>
    /// <param name="property">The PropertyInfo for the property to check.</param>
    /// <returns>True if the property is declared as nullable; otherwise, false.</returns>
    public static bool IsNullable(PropertyInfo property)
    
        => _context.Create(property).WriteState == NullabilityState.Nullable; 

    
}
