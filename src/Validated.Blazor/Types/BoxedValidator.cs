using Validated.Blazor.Common.Constants;

namespace Validated.Blazor.Types;

/// <summary>
/// Represents a type-erased container for a MemberValidator delegate and its associated metadata.
/// </summary>
/// <param name="ForMember">The name of the member this validator targets.</param>
/// <param name="ForType">
/// The context in which the validator operates (e.g., for a single member, a collection, or a comparison).
/// </param>
/// <param name="Optional">Indicates whether validation should be skipped if the member's value is null.</param>
/// <param name="MemberValidator">
/// The underlying validator delegate, stored as a generic <see cref="object"/>.
/// </param>
/// <param name="MemberType">
/// The specific <see cref="Type"/> of the member that the validator expects (e.g., typeof(string), typeof(int)).
/// </param>
/// <remarks>
/// <para>
/// The Blazor validation system needs to store validators for various property types in a single dictionary.
/// Since a <c>MemberValidator&lt;T&gt;</c> is a generic delegate, you cannot store <c>MemberValidator&lt;string&gt;</c>
/// and <c>MemberValidator&lt;int&gt;</c> in the same collection directly.
/// </para>
/// <para>
/// The <c>BoxedValidator</c> solves this problem by "boxing" the generic delegate into a non-generic record.
/// It stores the delegate as an <see cref="object"/> and captures its generic type argument in the <see cref="MemberType"/>
/// property. This allows the <c>BlazorValidated&lt;TEntity&gt;</c> component to later use reflection to cast the
/// validator back to its specific generic type and invoke it with the correct arguments.
/// </para>
/// </remarks>
public record BoxedValidator(string ForMember, ForType ForType, bool Optional, object MemberValidator, Type MemberType);

