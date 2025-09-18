using FluentAssertions;
using FluentAssertions.Execution;
using System.Collections.Immutable;
using Validated.Blazor.Builders;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Types;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Data;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;
using Validated.Core.Factories;
using Validated.Core.Types;

namespace Validated.Blazor.Tests.Integration.Builders;

public class BlazorTenantValidationBuilder_Tests
{

    private static BlazorTenantValidationBuilder<TEntity> CreateTenantBuilder<TEntity>() where TEntity : class

        => BlazorTenantValidationBuilder<TEntity>.Create(StaticData.ValidationRuleConfigsForTenantValidationBuilder(), new ValidatorFactoryProvider());

    [Fact]
    public void The_create_method_should_create_and_return_a_blazor_tenant_validation_builder()
    {
        var validatorFactoryProvider = new ValidatorFactoryProvider();
        var ruleConfigs = StaticData.ValidationRuleConfigsForTenantValidationBuilder();

        var tenantBuilder = BlazorTenantValidationBuilder<ContactDto>.Create(ruleConfigs, validatorFactoryProvider);

        tenantBuilder.Should().BeOfType<BlazorTenantValidationBuilder<ContactDto>>();

    }
    [Fact]
    public async Task The_for_member_should_add_a_validator_for_the_member()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForMember(c => c.Title);
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.Title);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);

        }
    }

    [Fact]
    public async Task The_for_nullable_member_should_be_able_to_add_a_validator_for_the_nullable_member()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var tenantBuilder = CreateTenantBuilder<ContactDto>().ForNullableMember(c => c.NullableAge);
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<int>)boxedValidators.ElementAt(0).Value.MemberValidator;

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
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForNullableStringMember(c => c.Mobile);
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.Mobile!);//data fails validation as its not a uk number

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == false && v.Failures.Count == 1);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_nested_member_should_be_able_to_add_a_validator_for_the_nested_entity()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var addressBuilder  = CreateTenantBuilder<AddressDto>().ForMember(a => a.AddressLine);
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForNestedMember(c => c.Address, addressBuilder.GetBoxedValidators());
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.Address.AddressLine);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_nullable_nested_member_should_be_able_to_add_a_validator_for_the_nested_entity()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var addressBuilder  = CreateTenantBuilder<AddressDto>().ForMember(a => a.AddressLine);
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForNullableNestedMember(c => c.NullableAddress, addressBuilder.GetBoxedValidators());
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        contactData.NullableAddress = new AddressDto() { AddressLine = "AddressLine"};

        var validator = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

        var validated = await validator(contactData.NullableAddress.AddressLine);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<string>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_each_collection_member_should_be_able_to_add_a_validator_for_the_collection()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var methodsBuilder  = CreateTenantBuilder<ContactMethodDto>().ForMember(a => a.MethodType);
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForEachCollectionMember(c => c.ContactMethods, methodsBuilder.GetBoxedValidators());
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<string>)boxedValidators.ElementAt(0).Value.MemberValidator;

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
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForComparisonWithMember(c => c.CompareDOB);
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<ContactDto>)boxedValidators.ElementAt(0).Value.MemberValidator;
        var validated = await validator(contactData);

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
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForComparisonWithValue(c => c.Age);
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        var validator = (MemberValidator<int>)boxedValidators.ElementAt(0).Value.MemberValidator;
        var validated = await validator(contactData.Age);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<int>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForMember);
        }
    }

    [Fact]
    public async Task The_for_collection_should_add_a_validator_to_validate_the_collection()
    {
        var contactData     = StaticData.CreateContactObjectGraph();
        var tenantBuilder   = CreateTenantBuilder<ContactDto>().ForCollection(c => c.Entries);
        var boxedValidators = tenantBuilder.GetBoxedValidators();

        contactData.Entries = ["EntryOne", "EntryTwo"];

        var validator = (MemberValidator<List<string>>)boxedValidators.ElementAt(0).Value.MemberValidator;
        var validated = await validator(contactData.Entries);

        using (new AssertionScope())
        {
            boxedValidators.Count.Should().Be(1);
            validated.Should().Match<Validated<List<string>>>(v => v.IsValid == true && v.Failures.Count == 0);
            boxedValidators.ElementAt(0).Value.ForType.Should().Be(ForType.ForCollection);
        }
    }

    [Fact]
    public void Adding_validators_for_nested_members_or_collections_should_adjust_the_dictionary_key_name_root_to_match_the_parent_property_name()
    {
        var addressValidation = CreateTenantBuilder<AddressDto>().ForMember(a => a.AddressLine);
        var contactValidation = BlazorValidationBuilder<ContactDto>.Create().ForNestedMember(c => c.Address, addressValidation.GetBoxedValidators());

        var originalKeyRoot = addressValidation.GetBoxedValidators().First().Key.Split(".")[0];
        var newKeyRoot      = contactValidation.GetBoxedValidators().First().Key.Split(".")[0];

        using (new AssertionScope())
        {
            originalKeyRoot.Should().Be(nameof(AddressDto));
            newKeyRoot.Should().Be(nameof(ContactDto.Address));
        }

    }
}