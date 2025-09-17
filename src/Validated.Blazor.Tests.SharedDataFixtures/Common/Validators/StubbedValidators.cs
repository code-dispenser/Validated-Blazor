using Validated.Core.Types;

namespace Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;

public static class StubbedValidators
{
    public static MemberValidator<T> CreatePassingMemberValidator<T>() where T : notnull

        => (value, path, compareTo, _) => Task.FromResult(Validated<T>.Valid(value));
    public static MemberValidator<T> CreateFailingMemberValidator<T>(string propertyName, string displayName, string failureMessage) where T : notnull

        => (value, path, compareTo, _) => Task.FromResult(Validated<T>.Invalid(new InvalidEntry(failureMessage, path, propertyName, displayName)));

}
