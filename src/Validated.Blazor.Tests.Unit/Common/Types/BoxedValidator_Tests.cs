using FluentAssertions;
using FluentAssertions.Execution;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;
using Validated.Blazor.Types;

namespace Validated.Blazor.Tests.Unit.Common.Types;

public class BoxedValidator_Tests
{
    [Fact]
    public void All_properties_should_be_set_via_the_constructor()
    {
        var boxedValidator = new BoxedValidator(nameof(ContactDto.GivenName), ForType.ForMember,false, (object)StubbedValidators.CreatePassingMemberValidator<string>, typeof(string));

        using(new AssertionScope())
        {
            boxedValidator.Should().Match<BoxedValidator>(b => b.ForMember == nameof(ContactDto.GivenName) && b.ForType == ForType.ForMember && b.Optional == false
                                                       && b.MemberValidator.GetType() ==  ((object)StubbedValidators.CreatePassingMemberValidator<string>).GetType()
                                                       && b.MemberType == typeof(string));
        }

    }

    [Fact]
    public void An_empty_with_expression_should_create_a_copy_of_the_boxed_validator()
    {
        var boxedValidator = new BoxedValidator(nameof(ContactDto.GivenName), ForType.ForMember, false, (object)StubbedValidators.CreatePassingMemberValidator<string>, typeof(string));

        var copyValidator = boxedValidator with { };

        copyValidator.Should().BeEquivalentTo(boxedValidator);

    }

    [Fact]
    public void With_expression_should_set_all_properties()
    {
        var boxedValidator = new BoxedValidator(nameof(ContactDto.GivenName), ForType.ForMember, false, (object)StubbedValidators.CreatePassingMemberValidator<string>, typeof(string));

        var newValidator = boxedValidator with { ForMember = nameof(ContactDto.Age), ForType = ForType.ForMember, Optional = true, MemberValidator = (object)StubbedValidators.CreatePassingMemberValidator<int>, MemberType = typeof(int) };


        newValidator.Should().Match<BoxedValidator>(b => b.ForMember == nameof(ContactDto.Age) && b.ForType == ForType.ForMember && b.Optional == true
                                                       && b.MemberValidator.GetType() ==  ((object)StubbedValidators.CreatePassingMemberValidator<int>).GetType()
                                                       && b.MemberType == typeof(int));


    }
}
