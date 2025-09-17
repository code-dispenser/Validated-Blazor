using FluentAssertions;
using FluentAssertions.Execution;
using Validated.Blazor.Builders;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;

namespace Validated.Blazor.Tests.Integration.Builders;

public class BlazorValidationBuilder_Tests
{
    [Fact]
    public void Adding_validators_for_nested_members_or_collections_should_adjust_the_dictionary_key_name_root_to_match_the_parent_property_name()
    {
        var addressValidation = BlazorValidationBuilder<AddressDto>.Create().ForMember(a => a.AddressLine, StubbedValidators.CreatePassingMemberValidator<string>());
        var contactValidation = BlazorValidationBuilder<ContactDto>.Create().ForNestedMember(c => c.Address,addressValidation.GetBoxedValidators());

        var originalKeyRoot = addressValidation.GetBoxedValidators().First().Key.Split(".")[0];
        var newKeyRoot      = contactValidation.GetBoxedValidators().First().Key.Split(".")[0];

        using(new AssertionScope())
        {
            originalKeyRoot.Should().Be(nameof(AddressDto));
            newKeyRoot.Should().Be(nameof(ContactDto.Address));
        }

    }
}
