using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Types;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Core.Validators;
using Xunit.Sdk;

namespace Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;

public static class BoxedValidators
{
    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactTitleValidator()
    {
        BoxedValidator validator = new(nameof(ContactDto.Title), ForType.ForMember, false, ContactModelValidators.TitleValidator, typeof(string));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(ContactDto),".", nameof(ContactDto.Title))] = validator}.ToImmutableDictionary();
    }
    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactGivenNameValidator()
    {
        BoxedValidator validator = new(nameof(ContactDto.Title), ForType.ForMember, false, ContactModelValidators.GivenNameValidator, typeof(string));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(ContactDto), ".", nameof(ContactDto.GivenName))] = validator }.ToImmutableDictionary();
    }

    public static ImmutableDictionary<string, BoxedValidator> TitleAndNestedAddressLineValidators()
    {
        //The builders replace nested names with the address property name of the parent.

        var addressLineKey  = "Address.AddressLine";
        var contactTitleKey = String.Concat(nameof(ContactDto), ".", nameof(ContactDto.Title));

        BoxedValidator titleValidator       = new(nameof(ContactDto.Title), ForType.ForMember, false, ContactModelValidators.GivenNameValidator, typeof(string));
        BoxedValidator addressLineValidator = new(nameof(AddressDto.AddressLine), ForType.ForMember, false, ContactModelValidators.AddressLineValidator, typeof(string));

        return new Dictionary<string, BoxedValidator>
        { 
            [contactTitleKey] = titleValidator,
            [addressLineKey]  = addressLineValidator,

        }.ToImmutableDictionary();

    }

    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactNullableAgeValidator()
    {
        BoxedValidator validator = new(nameof(ContactDto.NullableAge), ForType.ForMember, true, ContactModelValidators.NullableAgeValidator, typeof(int));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(ContactDto), ".", nameof(ContactDto.NullableAge))] = validator }.ToImmutableDictionary();
    }
    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactAgeValidator()
    {
        BoxedValidator validator = new(nameof(ContactDto.Age), ForType.ForMember, false, ContactModelValidators.AgeValidator, typeof(int));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(ContactDto), ".", nameof(ContactDto.Age))] = validator }.ToImmutableDictionary();
    }

    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactEntriesValidator()
    {
        BoxedValidator validator = new(nameof(ContactDto.Entries), ForType.ForCollection, false, ContactModelValidators.EntryCountValidator, typeof(List<string>));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(ContactDto), ".", nameof(ContactDto.Entries))] = validator }.ToImmutableDictionary();
    }

    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactOptionalEntriesValidator()//Only for test there is no equivalent for the builders
    {
        BoxedValidator validator = new(nameof(ContactDto.Entries), ForType.ForCollection, true, ContactModelValidators.EntryCountValidator, typeof(List<string>));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(ContactDto), ".", nameof(ContactDto.Entries))] = validator }.ToImmutableDictionary();
    }

    public static ImmutableDictionary<string, BoxedValidator> OnlyTheContactPrimitiveValidator()//ONly for test, this is not equivalent for the builders
    {
        var memberValidator = MemberValidators.CreateRangeValidator(1,3,"Number", "Number", "Must be between 1 and 3 inclusive");

        BoxedValidator validator = new(nameof(PrimitiveCollectionHolder.Numbers), ForType.ForMember,false,memberValidator, typeof(int));

        return new Dictionary<string, BoxedValidator> { [String.Concat(nameof(PrimitiveCollectionHolder.Numbers), ".", "Number")] = validator }.ToImmutableDictionary();
    }
}
