using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Types;
using Validated.Blazor.Common.Utilities;
using Validated.Core.Common.Constants;
using Validated.Core.Factories;
using Validated.Core.Types;

namespace Validated.Blazor.Builders;


/// <summary>
/// A builder for composing Blazor-compatible validators from configuration with tenant and culture support.
/// </summary>
/// <typeparam name="TEntity">The type of entity for which validation rules are being built.</typeparam>
/// <remarks>
/// <para>
/// The BlazorTenantValidationBuilder provides a fluent interface for creating a collection of validators
/// designed to be used with the <see cref="BlazorValidated{TEntity}"/> component. Unlike its counterpart
/// in Validated.Core, this builder produces an <see cref="ImmutableDictionary{TKey, TValue}"/> of
/// <see cref="BoxedValidator"/> instances rather than a single <see cref="EntityValidator{TEntity}"/> delegate.
/// </para>
/// <para>
/// It uses a factory provider to create validators from rule configurations, automatically resolving
/// the most appropriate rules based on the provided tenant and culture identifiers. This enables dynamic,
/// configuration-driven validation in multi-tenant Blazor applications.
/// </para>
/// </remarks>
public class BlazorTenantValidationBuilder<TEntity> where TEntity : notnull
{
    private readonly Dictionary<string, BoxedValidator> _boxedValidators = [];

    private readonly HashSet<string>            _membersAdded;
    private readonly IValidatorFactoryProvider  _factoryProvider;
    private readonly string                     _entityTypeFullName;
    private readonly string                     _tenantID;
    private readonly string                     _cultureID;

    private readonly ImmutableList<ValidationRuleConfig> _ruleConfigs = [];


    /// <summary>
    /// Initializes a new instance of the BlazorTenantValidationBuilder with the specified configuration and context.
    /// </summary>
    /// <param name="ruleConfigs">The complete set of validation rule configurations available for creating validators.</param>
    /// <param name="factoryProvider">The factory provider responsible for creating validators from rule configurations.</param>
    /// <param name="tenantID">The tenant identifier for multi-tenant rule resolution. Defaults to the system default tenant.</param>
    /// <param name="cultureID">The culture identifier for localized validation rules and messages. Defaults to en-GB. </param>
    internal BlazorTenantValidationBuilder(ImmutableList<ValidationRuleConfig> ruleConfigs, IValidatorFactoryProvider factoryProvider, string tenantID = ValidatedConstants.Default_TenantID,
        string cultureID = ValidatedConstants.Default_CultureID)
    {
        _membersAdded       = [];
        _factoryProvider    = factoryProvider;
        _entityTypeFullName = typeof(TEntity).FullName!;
        _tenantID           = tenantID;
        _cultureID          = cultureID;
        _ruleConfigs        = ruleConfigs;
    }

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
    /// Configures validation for a non-nullable property using validators created from rule configurations.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the property to validate.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorTenantValidationBuilder<TEntity> ForMember<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression) where TProperty : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey  = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);
        var validator  = _factoryProvider.CreateValidator<TProperty>(_entityTypeFullName, memberName, _ruleConfigs, _tenantID, _cultureID);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, false);

        return this;
    }

    /// <summary>
    /// Configures validation for a nullable value type property, skipping validation if the value is null.
    /// </summary>
    /// <typeparam name="TProperty">The value type of the property to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the nullable property to validate.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorTenantValidationBuilder<TEntity> ForNullableMember<TProperty>(Expression<Func<TEntity, TProperty?>> selectorExpression) where TProperty : struct
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey  = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);
        var validator  = _factoryProvider.CreateValidator<TProperty>(_entityTypeFullName, memberName, _ruleConfigs, _tenantID, _cultureID);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, true);

        return this;
    }

    /// <summary>
    /// Configures validation for a nullable string property, skipping validation if the value is null.
    /// </summary>
    /// <param name="selectorExpression">An expression that selects the nullable string property to validate.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorTenantValidationBuilder<TEntity> ForNullableStringMember(Expression<Func<TEntity, string?>> selectorExpression)
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey  = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);
        var validator  = _factoryProvider.CreateValidator<string>(_entityTypeFullName, memberName, _ruleConfigs, _tenantID, _cultureID);

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
    public BlazorTenantValidationBuilder<TEntity> ForNestedMember<TNested>(Expression<Func<TEntity, TNested>> selectorExpression, ImmutableDictionary<string, BoxedValidator> boxedValidators) where TNested : notnull
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
    public BlazorTenantValidationBuilder<TEntity> ForNullableNestedMember<TNested>(Expression<Func<TEntity, TNested?>> selectorExpression, ImmutableDictionary<string, BoxedValidator> boxedValidators) where TNested : class
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
    public BlazorTenantValidationBuilder<TEntity> ForEachCollectionMember<TItem>(Expression<Func<TEntity, IEnumerable<TItem>>> selectorExpression, ImmutableDictionary<string, BoxedValidator> boxedValidators) where TItem : notnull
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
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorTenantValidationBuilder<TEntity> ForComparisonWithMember<TMember>(Expression<Func<TEntity, TMember>> selectorExpression) where TMember : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        var validator = _factoryProvider.CreateValidator<TEntity>(_entityTypeFullName, memberName, _ruleConfigs, _tenantID, _cultureID);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForComparison, false);

        return this;
    }

    /// <summary>
    /// Configures validation that compares a member's value against a configured value.
    /// </summary>
    /// <typeparam name="TMember">The type of the member property to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the member property to validate.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorTenantValidationBuilder<TEntity> ForComparisonWithValue<TMember>(Expression<Func<TEntity, TMember>> selectorExpression) where TMember : notnull
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);

        var validator = _factoryProvider.CreateValidator<TMember>(_entityTypeFullName, memberName, _ruleConfigs, _tenantID, _cultureID);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForMember, false);

        return this;
    }

    /// <summary>
    /// Configures validation for a collection property as a whole (e.g., its size).
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection to validate.</typeparam>
    /// <param name="selectorExpression">An expression that selects the collection property.</param>
    /// <returns>The current builder instance for fluent method chaining.</returns>
    public BlazorTenantValidationBuilder<TEntity> ForCollection<TCollection>(Expression<Func<TEntity, TCollection>> selectorExpression) where TCollection : notnull, IEnumerable
    {
        var memberName = GeneralUtils.GetMemberName(selectorExpression);
        var memberKey = GeneralUtils.BuildMemberValidatorKey(typeof(TEntity).Name, memberName);
        var validator = _factoryProvider.CreateValidator<TCollection>(_entityTypeFullName, memberName, _ruleConfigs, _tenantID, _cultureID);

        AddBoxedValidator(memberName, memberKey, validator, ForType.ForCollection, false);

        return this;
    }

    /// <summary>
    /// Creates a new instance of the BlazorTenantValidationBuilder.
    /// </summary>
    /// <param name="ruleConfigs">The complete set of validation rule configurations.</param>
    /// <param name="factoryProvider">The factory provider for creating validators.</param>
    /// <param name="tenantID">The tenant identifier for rule resolution.</param>
    /// <param name="cultureID">The culture identifier for rule resolution.</param>
    /// <returns>A new <see cref="BlazorTenantValidationBuilder{TEntity}"/> instance.</returns>
    public static BlazorTenantValidationBuilder<TEntity> Create(ImmutableList<ValidationRuleConfig> ruleConfigs, IValidatorFactoryProvider factoryProvider, string tenantID = ValidatedConstants.Default_TenantID, string cultureID = ValidatedConstants.Default_CultureID)

        => new(ruleConfigs, factoryProvider, tenantID, cultureID);

    /// <summary>
    /// Builds and returns the final dictionary of boxed validators.
    /// </summary>
    /// <returns>An <see cref="ImmutableDictionary{TKey, TValue}"/> mapping member keys to their <see cref="BoxedValidator"/> instances.</returns>
    public ImmutableDictionary<string, BoxedValidator> GetBoxedValidators()

        => _boxedValidators.ToImmutableDictionary();
}
