namespace Validated.Blazor.Common.Constants;

/// <summary>
/// Specifies the operational context for a validator.
/// </summary>
public enum ForType : int
{
    /// <summary>
    /// The validator applies to a single property of a model.
    /// </summary>
    ForMember,

    /// <summary>
    /// The validator applies to an entire collection (e.g., checking its size).
    /// </summary>
    ForCollection,

    /// <summary>
    /// The validator performs a comparison, typically involving the entire entity.
    /// </summary>
    ForComparison
};

/// <summary>
/// Specifies the scope of a validation request.
/// </summary>
public enum ValidationLevel : int
{
    /// <summary>
    /// The validation request is for a single field, usually triggered by an `OnFieldChanged` event.
    /// </summary>
    Field,

    /// <summary>
    /// The validation request is for the entire model, usually triggered by a form submission.
    /// </summary>
    Model
};
