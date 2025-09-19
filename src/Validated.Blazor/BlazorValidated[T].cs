using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Utilities;
using Validated.Blazor.Types;
using Validated.Core.Types;

namespace Validated.Blazor;

/// <summary>
/// A Blazor component that integrates the Validated.Core validation library with Blazor's EditContext.
/// </summary>
/// <typeparam name="TEntity">The type of the model being validated.</typeparam>
/// <remarks>
/// <para>
/// The BlazorValidated component acts as the bridge between the core validation logic and the Blazor UI.
/// It hooks into the EditContext's validation events (OnValidationRequested for full-model validation and
/// OnFieldChanged for single-field validation) to trigger the appropriate validators.
/// </para>
/// <para>
/// It requires a dictionary of <see cref="BoxedValidator"/> instances, which are typically created using
/// <see cref="Builders.BlazorValidationBuilder{TEntity}"/> or <see cref="Builders.BlazorTenantValidationBuilder{TEntity}"/>.
/// This dictionary maps property identifiers to their corresponding type-erased validators, allowing the component
/// to dynamically invoke the correct validation logic for any field in the model.
/// </para>
/// <para>
/// Validation results are then published to the EditContext's <see cref="ValidationMessageStore"/>,
/// enabling standard Blazor components like <c>ValidationSummary</c> and <c>ValidationMessage</c> to display errors.
/// </para>
/// </remarks>
public class BlazorValidated<TEntity> : ComponentBase, IDisposable where TEntity : notnull
{
    [CascadingParameter] private EditContext CurrentEditContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection of boxed validators for the entity.
    /// </summary>
    /// <remarks>
    /// This dictionary is the core of the validation process, mapping member identifiers (e.g., "Model.PropertyName")
    /// to their respective <see cref="BoxedValidator"/> instances. It must be provided and is typically
    /// constructed using one of the Blazor-specific validation builders.
    /// </remarks>
    [Parameter, EditorRequired] public ImmutableDictionary<string, BoxedValidator> BoxedValidators    { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether to prefix validation messages with the property's display name.
    /// </summary>
    /// <remarks>
    /// When true, each validation message will be in the format "{DisplayName} - {FailureMessage}".
    /// </remarks>
    [Parameter] public bool AddDisplayName { get; set; } = true;

    /// <summary>
    /// An optional callback that is invoked when a validation operation begins.
    /// </summary>
    /// <remarks>
    /// This callback provides the <see cref="ValidationLevel"/> (Field or Model) and an optional <see cref="FieldIdentifier"/>.
    /// It can return a <see cref="CancellationToken"/> to support cancellation of the validation process.
    /// </remarks>
    [Parameter] public Func<ValidationLevel, FieldIdentifier?, Task<CancellationToken>>? OnValidationStarted   { get; set; }

    /// <summary>
    /// An optional callback that is invoked when a validation operation completes.
    /// </summary>
    /// <remarks>
    /// This callback receives the <see cref="ValidationLevel"/>, an optional <see cref="FieldIdentifier"/>,
    /// and the <see cref="CancellationToken"/> that was used for the operation, allowing for post-validation logic.
    /// </remarks>
    [Parameter] public Func<ValidationLevel, FieldIdentifier?, CancellationToken, Task>? OnValidationCompleted { get; set; }

    private ValidationMessageStore _messageStore    = default!;
    private const string           _prefixSeparator = " - ";

    /// <summary>
    /// Initializes the component by attaching event handlers to the <see cref="EditContext"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the component is not placed within an <c>EditForm</c> or if the <see cref="BoxedValidators"/> parameter is null or empty.
    /// </exception>
    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)  throw new InvalidOperationException(ErrorMessages.Validator_Missing_Context_Message);

        if (BoxedValidators == null || BoxedValidators.IsEmpty) throw new InvalidOperationException(ErrorMessages.Validator_Missing_Boxed_Validators_Message);

        _messageStore = new ValidationMessageStore(CurrentEditContext);

        CurrentEditContext.OnFieldChanged        += CurrentEditContext_OnFieldChanged;
        CurrentEditContext.OnValidationRequested += CurrentEditContext_OnValidationRequested;
    }

    /// <summary>
    /// Handles the full model validation request from the EditContext.
    /// </summary>
    private async void CurrentEditContext_OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        var cancellationToken = CancellationToken.None;

        if (OnValidationStarted is not null)
        {
            try
            {
                cancellationToken = await OnValidationStarted(ValidationLevel.Model, null);
            }
            catch { }
        }

        _messageStore.Clear();

        MethodInfo methodInfo = typeof(BlazorValidated<TEntity>).GetMethod("ValidateModal", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(TEntity), typeof(ValidatedContext), typeof(CancellationToken)])!;

        _ = await((Task<Validated<TEntity>>)methodInfo.Invoke(this, [CurrentEditContext.Model, default, cancellationToken])!);

        CurrentEditContext.NotifyValidationStateChanged();
        
        if (OnValidationCompleted is not null) await OnValidationCompleted(ValidationLevel.Model, null, cancellationToken);      
    }

    /// <summary>
    /// Handles the validation of a single field when its value changes in the EditContext.
    /// </summary>
    private async void CurrentEditContext_OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        var cancellationToken = CancellationToken.None;
        
        if(OnValidationStarted is not null)
        {
            try
            {
                cancellationToken = await OnValidationStarted(ValidationLevel.Field, e.FieldIdentifier); 
            }
            catch { }
        }
   
        var rootName = e.FieldIdentifier.Model == CurrentEditContext.Model 
                            ? typeof(TEntity).Name
                                : GeneralUtils.FindModelPropertyName(CurrentEditContext.Model, e.FieldIdentifier.Model, new HashSet<object>(ReferenceEqualityComparer.Instance));

        var memberKey = String.Concat(rootName, ".", e.FieldIdentifier.FieldName);

        if (false == BoxedValidators.TryGetValue(memberKey, out var boxedValidator)) return;

        PropertyInfo propertyInfo  = e.FieldIdentifier.Model.GetType().GetProperty(e.FieldIdentifier.FieldName)!;
        MethodInfo   methodInfo    = typeof(BlazorValidated<TEntity>).GetMethod("ValidateField", BindingFlags.NonPublic | BindingFlags.Static, [typeof(BoxedValidator),typeof(string), typeof(object), typeof(TEntity), typeof(CancellationToken)])!;
        MethodInfo   genericMethod = methodInfo.MakeGenericMethod(boxedValidator.MemberType);

        var validated = await ((Task<Validated<TEntity>>)genericMethod.Invoke(this, [boxedValidator, e.FieldIdentifier.FieldName, e.FieldIdentifier.Model, CurrentEditContext.Model, cancellationToken])!);

        _messageStore.Clear(e.FieldIdentifier);

        if (true == validated.IsInvalid) _messageStore.Add(e.FieldIdentifier, SetFailureMessages(AddDisplayName,validated.Failures,_prefixSeparator));
        
        CurrentEditContext.NotifyValidationStateChanged();

        if (OnValidationCompleted is not null) await OnValidationCompleted(ValidationLevel.Field, e.FieldIdentifier, cancellationToken);
    }

    /// <summary>
    /// Detaches event handlers from the EditContext.
    /// </summary>
    private void RemoveFromEditContext()
    {
        if (CurrentEditContext != null)
        {
            CurrentEditContext.OnFieldChanged        -= CurrentEditContext_OnFieldChanged;
            CurrentEditContext.OnValidationRequested -= CurrentEditContext_OnValidationRequested;
            
            _messageStore?.Clear();
        }
    }

    /// <summary>
    /// Recursively validates the entire model and its nested properties.
    /// </summary>
    /// <param name="formModal">The model instance to validate.</param>
    /// <param name="context">The validation context for tracking state like circular references.</param>
    /// <param name="cancellationToken">A token to signal cancellation of the validation operation.</param>
    /// <returns>A <see cref="Validated{TEntity}"/> result containing either the valid model or a collection of failures.</returns>
    private async Task<Validated<TEntity>> ValidateModal(TEntity formModal, ValidatedContext? context = null, CancellationToken cancellationToken = default)
    {
        List<InvalidEntry> failures = [];
        var currentModel = formModal;
        PropertyInfo[] properties = formModal.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (context == null) context = new ValidatedContext();
       
        await RecurseModel(currentModel, currentModel.GetType().Name);

        async Task RecurseModel(object currentModel, string rootName)
        {
            if (context.IsValidating(currentModel)) return;
            
            context = context.WithValidating(currentModel);

            PropertyInfo[] properties = currentModel.GetType().GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                var fieldName    = propertyInfo.Name;
                var propertyType = propertyInfo.PropertyType;
                string keyName = $"{rootName}.{fieldName}";

                if (true == propertyType.IsClass && propertyType.GetInterface(nameof(IEnumerable)) != null && propertyType != typeof(string))
                {
                    IEnumerable? items = propertyInfo.GetValue(currentModel) as IEnumerable;

                    if (true == BoxedValidators.TryGetValue(keyName, out var boxedValidator) && (boxedValidator.ForType == ForType.ForCollection))
                    {
                        if (items is null && boxedValidator.Optional == true) continue;//not implemented but made test to exercise the continue

                        var entityFailures = await CallValidateField(boxedValidator, fieldName, items!, (TEntity)CurrentEditContext.Model, cancellationToken);

                        if (entityFailures.Count > 0)
                        {
                            _messageStore.Add(new FieldIdentifier(currentModel, fieldName), SetFailureMessages(AddDisplayName, entityFailures, _prefixSeparator));

                            failures.AddRange(entityFailures);
                        }
                    }

                    if (items == null) continue;
                    
                    if (!items.Cast<object>().Any()) continue;//Code coverage did not like a one liner and showed it in orange
                    
                    foreach (var item in items!)
                    {
                        var itemType = item.GetType();
                        if (itemType.IsPrimitive || itemType == typeof(string)) continue;//not supported

                        await RecurseModel(item, fieldName);
                    }
                }

                if (true == propertyType.IsClass && propertyType != typeof(string) && propertyType.GetInterface(nameof(IEnumerable)) == null)
                {
                    var value = propertyInfo.GetValue(currentModel);

                    if (value == null)
                    {
                        if (false == GeneralUtils.IsNullable(propertyInfo))
                        {
                            failures.Add(new InvalidEntry($"{fieldName} cannot be null", "", fieldName, fieldName));
                            _messageStore!.Add(new FieldIdentifier(currentModel, fieldName), $"{fieldName} cannot be null");
                        }
                        continue;
                    }
                    await RecurseModel(value, fieldName);
                }

                if (BoxedValidators.TryGetValue(keyName, out var validator) && validator.ForType != ForType.ForCollection)
                {
                    var valueFailures = await CallValidateField(validator, fieldName, currentModel, (TEntity)CurrentEditContext.Model, cancellationToken);

                    if (valueFailures.Count > 0)
                    {
                        _messageStore!.Add(new FieldIdentifier(currentModel, fieldName), SetFailureMessages(AddDisplayName, valueFailures, _prefixSeparator));

                        failures.AddRange(valueFailures);
                    }
                }
            }

        }
        return failures.Count > 0 ? Validated<TEntity>.Invalid(failures) : Validated<TEntity>.Valid(formModal);
    }

    /// <summary>
    /// Invokes the generic `ValidateField` method using reflection.
    /// </summary>
    /// <param name="boxedValidator">The boxed validator containing the validator.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <param name="fieldModel">The model instance that owns the field.</param>
    /// <param name="formModel">The root model of the form.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of validation failures; empty if validation succeeds.</returns>
    private async Task<List<InvalidEntry>> CallValidateField(BoxedValidator boxedValidator, string fieldName, object fieldModel, TEntity formModel, CancellationToken cancellationToken = default)
    {
        MethodInfo methodInfo    = typeof(BlazorValidated<TEntity>).GetMethod("ValidateField", BindingFlags.NonPublic | BindingFlags.Static, [typeof(BoxedValidator), typeof(string), typeof(object), typeof(TEntity), typeof(CancellationToken)])!;
        MethodInfo genericMethod = methodInfo.MakeGenericMethod(boxedValidator.MemberType);

        var result =  await ((Task<Validated<TEntity>>)genericMethod.Invoke(this, [boxedValidator, fieldName,fieldModel, formModel, cancellationToken])!);

        return result.IsValid ? [] : [.. result.Failures];
    }

    /// <summary>
    /// Performs the validation for a single field.
    /// </summary>
    /// <typeparam name="TMemberType">The type of the member being validated.</typeparam>
    /// <param name="boxedValidator">The boxed validator for the member.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="fieldModel">The model containing the field.</param>
    /// <param name="formModel">The root form model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Validated{TEntity}"/> result for the operation.</returns>
    private static async Task<Validated<TEntity>> ValidateField<TMemberType>(BoxedValidator boxedValidator, string fieldName, object fieldModel, TEntity formModel, CancellationToken cancellationToken = default) where TMemberType : notnull
    {
        //object? memberValue = formModel is null ? null : fieldModel.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(fieldModel) ?? null;

        object? memberValue = fieldModel?.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(fieldModel);

        if (memberValue is null && boxedValidator.Optional) return Validated<TEntity>.Valid(formModel!);
       
        var memberType      = boxedValidator.MemberType;
        var memberValidator = (MemberValidator<TMemberType>)boxedValidator.MemberValidator;
        var objectValue     =  boxedValidator.ForType == ForType.ForMember ? memberValue : fieldModel;

        return (await memberValidator((TMemberType)objectValue!, "", default, cancellationToken)).Map(_ => formModel!);
    }

    /// <summary>
    /// Formats a collection of failure entries into user-friendly strings.
    /// </summary>
    /// <param name="addDisplayName">Whether to prefix with the display name.</param>
    /// <param name="failures">The collection of validation failures.</param>
    /// <param name="prefixSeparator">The separator to use between display name and message.</param>
    /// <returns>An enumerable of formatted error messages.</returns>
    private static IEnumerable<String> SetFailureMessages(bool addDisplayName, IEnumerable<InvalidEntry> failures, string prefixSeparator)

        => addDisplayName ? failures.Select(f => String.Concat(f.DisplayName, prefixSeparator, f.FailureMessage)) : failures.Select(f => f.FailureMessage);

    /// <summary>
    /// Disposes the component by removing event handlers.
    /// </summary>
    public void Dispose()

        => RemoveFromEditContext();
}
