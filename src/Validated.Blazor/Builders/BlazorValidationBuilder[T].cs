using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Types;
using Validated.Blazor.Common.Utilities;
using Validated.Core.Types;

namespace Validated.Blazor.Builders;

/// <summary>
/// A builder for composing Blazor-compatible validators by explicitly providing validator delegates.
/// </summary>
/// <typeparam name="TEntity">The type of entity for which validation rules are being built.</typeparam>
/// <remarks>
/// <para>
/// The BlazorValidationBuilder provides a fluent interface for manually constructing a collection of validators
/// for use with the <see cref="BlazorValidated{TEntity}"/> component. Unlike its counterpart,
/// <see cref="BlazorTenantValidationBuilder{TEntity}"/>, this builder does not use a configuration source.
/// Instead, it requires you to provide a <see cref="MemberValidator{T}"/> delegate for each property.
/// </para>
/// <para>
/// This approach offers maximum flexibility for custom, runtime-defined, or complex validation logic that
/// may not fit a configuration-driven model. Like the tenant builder, its final output is an
/// <see cref="ImmutableDictionary{TKey, TValue}"/> of <see cref="BoxedValidator"/> instances, which is
/// the required format for the Blazor component.
/// </para>
/// </remarks>
public class BlazorValidationBuilder<TEntity> where TEntity : notnull
{
    private readonly Dictionary<string, BoxedValidator> _boxedValidators = [];

    /// <summary>
    /// Initializes a new instance of the BlazorValidationBuilder.
    /// </summary>
    /// <remarks>
    /// The constructor is internal to encourage the use of the static <see cref="Create"/> factory method.
    /// </remarks>
    internal BlazorValidationBuilder() { }

    /// <summary>
    /// Adds a single, type-erased validator to the dictionary.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    /// <param name="memberName">The name of the member property.</param>
    /// <param name="memberValidatorKey">The unique key for the validator in the dictionary (e.g., "Model.Property").</param>
    /// <param name="validator">The member validator delegate.</param>
    /// <param name="forType">The context in which the validator operates (e.g., member, collection).</param>
    /// <param name="optional">Indicates if validation should be skipped if the value is null.</param>
    private void AddBoxedValidator<TProperty>(string memberName, string memberValidatorKey, MemberValidator<TProperty> validator, ForType forType = ForType.ForMember, bool optional = false) where TProperty : notnull

        => _boxedValidators[memberValidatorKey] = new BoxedValidator(memberName, forType, optional, validator, typeof(TProperty));

    /// <summary>
    /// Adds a collection of boxed validators for a nested member to the main dictionary.
    /// </summary>
    /// <param name="boxedValidators">The dictionary of validators for the nested object.</param>
    /// <param name="memberName">The name of the nested member property in the parent entity.</param>
    private void AddBoxedValidators(ImmutableDictionary<string, BoxedValidator> boxedValidators, string memberName)
    {
        foreach (var keyPair in boxedValidators)
        {
            var keyNameParts = keyPair.Key.Split(".");
            var keyName = String.Concat(memberName, ".", keyNameParts[1]);

            _boxedValidators[keyName] = keyPair.Value;
        }
    }

    /// <summary>
    /// Configures validation for a non-nullable property using an explicitly provided validator.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the property to validate.</param>
    /// <param name="validator">The member validator delegate to apply to the property.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForMember<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression, MemberValidator<TProperty> validator) where TProperty : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey  = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, false);

        return this;
    }

    /// <summary>
    /// Configures validation for a nullable value type property, skipping validation if the value is null.
    /// </summary>
    /// <typeparam name="TProperty">The value type of the property to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the nullable property to validate.</param>
    /// <param name="validator">The member validator delegate to apply if the property has a value.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForNullableMember<TProperty>(Expression<Func<TEntity, TProperty?>> selectorExpression, MemberValidator<TProperty> validator) where TProperty : struct
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, true);

        return this;
    }

    /// <summary>
    /// Configures validation for a nullable string property, skipping validation if the value is null.
    /// </summary>
    /// <param name="selectorExpression">An expression that selects the nullable string property to validate.</param>
    /// <param name="validator">The member validator delegate to apply if the string is not null.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForNullableStringMember(Expression<Func<TEntity, string?>> selectorExpression, MemberValidator<string> validator)
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, true);

        return this;
    }

    /// <summary>
    /// Configures validation for a nested entity property by incorporating its validators.
    /// </summary>
    /// <typeparam name="TNested">The type of the nested entity.</typeparam>
    /// <param name="selectorExpression">An expression that selects the nested entity property.</param>
    /// <param name="boxedValidators">An immutable dictionary of validators for the nested entity.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForNestedMember<TNested>(Expression<Func<TEntity, TNested>> selectorExpression, ImmutableDictionary<string, BoxedValidator> boxedValidators) where TNested : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        AddBoxedValidators(boxedValidators, memberName);
        return this;
    }

    /// <summary>
    /// Configures validation for a nullable nested entity property, skipping validation if null.
    /// </summary>
    /// <typeparam name="TNested">The type of the nested entity.</typeparam>
    /// <param name="selectorExpression">An expression that selects the nullable nested entity property.</param>
    /// <param name="boxedValidators">An immutable dictionary of validators for the nested entity.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForNullableNestedMember<TNested>(Expression<Func<TEntity, TNested?>> selectorExpression, ImmutableDictionary<string, BoxedValidator> boxedValidators) where TNested : class
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        AddBoxedValidators(boxedValidators, memberName);
        return this;
    }

    /// <summary>
    /// Configures validation for each item in a collection property by incorporating validators for the item type.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the collection.</typeparam>
    /// <param name="selectorExpression">An expression that selects the collection property.</param>
    /// <param name="boxedValidators">An immutable dictionary of validators for the item type.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForEachCollectionMember<TItem>(Expression<Func<TEntity, IEnumerable<TItem>>> selectorExpression, ImmutableDictionary<string, BoxedValidator> boxedValidators) where TItem : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        AddBoxedValidators(boxedValidators, memberName);
        return this;
    }

    /// <summary>
    /// Configures comparison validation that compares one entity member to another.
    /// </summary>
    /// <typeparam name="TMember">The type of the member providing comparison context.</typeparam>
    /// <param name="selectorExpression">An expression that selects the member for comparison context.</param>
    /// <param name="validator">The entity validator that performs the comparison logic.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForComparisonWithMember<TMember>(Expression<Func<TEntity, TMember>> selectorExpression, MemberValidator<TEntity> validator) where TMember : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForComparison, false);

        return this;
    }

    /// <summary>
    /// Configures validation that compares a member's value against another value.
    /// </summary>
    /// <typeparam name="TProperty">The type of the member property to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the member property to validate.</param>
    /// <param name="validator">The member validator that performs the comparison logic.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForComparisonWithValue<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression, MemberValidator<TProperty> validator) where TProperty : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, false);

        return this;
    }

    /// <summary>
    /// Configures validation for a collection property as a whole (e.g., its size).
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the collection property.</param>
    /// <param name="validator">The member validator to apply to the entire collection.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorValidationBuilder<TEntity> ForCollection<TCollection>(Expression<Func<TEntity, TCollection>> selectorExpression, MemberValidator<TCollection> validator) where TCollection : notnull, IEnumerable
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForCollection, false);

        return this;
    }

    /// <summary>
    /// Creates a new instance of the BlazorValidationBuilder.
    /// </summary>
    /// <returns>A new <see cref="BlazorValidationBuilder{TEntity}"/> instance.</returns>
    public static BlazorValidationBuilder<TEntity> Create()

        => new();

    /// <summary>
    /// Builds and returns the final dictionary of boxed validators.
    /// </summary>
    /// <returns>An <see cref="ImmutableDictionary{TKey, TValue}"/> mapping member keys to their <see cref="BoxedValidator"/> instances.</returns>
    public ImmutableDictionary<string, BoxedValidator> GetBoxedValidators()

        => _boxedValidators.ToImmutableDictionary();

}