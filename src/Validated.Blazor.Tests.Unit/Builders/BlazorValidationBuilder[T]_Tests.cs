using FluentAssertions;
using FluentAssertions.Execution;
using System.Collections.Immutable;
using Validated.Blazor.Builders;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Types;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Data;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;
using Validated.Core.Builders;
using Validated.Core.Types;

namespace Validated.Blazor.Tests.Unit.Builders;

public class BlazorValidationBuilder_Tests
{
    [Fact]
    public void The_create_method_should_create_and_return_a_blazor_validation_builder()

        => BlazorValidationBuilder<ContactDto>.Create().Should().BeOfType<BlazorValidationBuilder<ContactDto>>();


    [Fact]
    public async Task The_for_member_should_add_a_validator_for_the_member()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var memberValidator = StubbedValidators.CreatePassingMemberValidator<string>();
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForMember(c => c.GivenName, memberValidator);
        
        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;
        
        var validated       = await validator(contactData.Title);

        using(new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);

        }
    }

    [Fact]
    public async Task The_for_nullable_member_should_be_able_to_add_a_validator_for_the_nullable_member()
    {
        var contactData      = StaticData.CreateContactObjectGraph();
        var memberValidator = StubbedValidators.CreatePassingMemberValidator<int>();
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForNullableMember(c => c.NullableAge, memberValidator);

        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<int>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator((int)contactData.NullableAge!);//cant use <int?> validators do not allow it

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<int>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_nullable_string_member_should_be_able_to_add_a_validator_for_the_nullable_string_member()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var memberValidator = StubbedValidators.CreatePassingMemberValidator<string>();
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForNullableStringMember(c => c.Mobile, memberValidator);

        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated       = await validator(contactData.Mobile!);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_nested_member_should_be_able_to_add_a_validator_for_the_nested_entity()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var nestedValidator = new BoxedValidator("AddressLine", ForType.ForMember, false, StubbedValidators.CreatePassingMemberValidator<AddressDto>(), typeof(AddressDto));
        var storedValidator = ImmutableDictionary.CreateRange(new Dictionary<string, BoxedValidator>{["Address.AddressLine"] = nestedValidator });

        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForNestedMember(c => c.Address!,storedValidator);

        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<AddressDto>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.Address!);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<AddressDto>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_nullable_nested_member_should_be_able_to_add_a_validator_for_the_nested_entity()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var nestedValidator = new BoxedValidator("AddressLine", ForType.ForMember, true, StubbedValidators.CreatePassingMemberValidator<AddressDto>(), typeof(AddressDto));
        var storedValidator = ImmutableDictionary.CreateRange(new Dictionary<string, BoxedValidator> { ["NullableAddress.AddressLine"] = nestedValidator });

        contactData.NullableAddress = new AddressDto();

        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForNullableNestedMember(c => c.NullableAddress!, storedValidator);
        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<AddressDto>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.NullableAddress!);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<AddressDto>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_each_collection_member_should_be_able_to_add_a_validator_for_the_collection()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var nestedValidator = new BoxedValidator("MethodType", ForType.ForMember, true, StubbedValidators.CreatePassingMemberValidator<string>(), typeof(string));
        var storedValidator = ImmutableDictionary.CreateRange(new Dictionary<string, BoxedValidator> { ["ContactMethods.MethodType"] = nestedValidator });
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForEachCollectionMember(c => c.ContactMethods, storedValidator);
        
        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.ContactMethods[0].MethodValue);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_comparison_with_member_should_add_a_validator_to_compare_members()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var memberValidator = StubbedValidators.CreatePassingMemberValidator<ContactDto>();
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForComparisonWithMember(c => c.DOB, memberValidator);

        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<ContactDto>)boxedValidators.ElementAt(0).Value.MemberValidator;
        var validated       = await validator(contactData);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<ContactDto>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForComparison);
        }
    }

    [Fact]
    public async Task The_for_comparison_with_value_should_add_a_validator_to_compare_a_member_to_a_value()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var memberValidator = StubbedValidators.CreatePassingMemberValidator<DateOnly>();
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForComparisonWithValue(c => c.DOB, memberValidator);

        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<DateOnly>)boxedValidators.ElementAt(0).Value.MemberValidator;
        var validated       = await validator(contactData.DOB);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<DateOnly>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_collection_should_add_a_validator_to_validate_the_collection()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var memberValidator = StubbedValidators.CreatePassingMemberValidator<List<string>>();
        var builder         = BlazorValidationBuilder<ContactDto>.Create().ForCollection(c => c.Entries, memberValidator);

        var boxedValidators = builder.GetBoxedValidators();
        var validator       = (MemberValidator<List<string>>)boxedValidators.ElementAt(0).Value.MemberValidator;
        var validated       = await validator(contactData.Entries);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<List<string>>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForCollection);
        }
    }
}
