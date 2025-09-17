using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Immutable;
using Validated.Blazor.Builders;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Types;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Data;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;

namespace Validated.Blazor.Tests.Integration;

public class BlazorValidated_Tests
{
    private static IRenderedComponent<BlazorValidated<ContactDto>> CreateValidatorComponent(TestContext context, EditContext editContext, ImmutableDictionary<string, BoxedValidator> boxedValidators,
                                                                                            bool addDisplayName = true, Func<ValidationLevel, FieldIdentifier?, Task<CancellationToken>>? onValidationStarted = null,
                                                                                            Func<ValidationLevel, FieldIdentifier?, CancellationToken, Task>? onValidationCompleted = null)

        => context.RenderComponent<BlazorValidated<ContactDto>>(paramBuilder => paramBuilder.AddCascadingValue(editContext)
                                                            .Add(paramBuilder => paramBuilder.BoxedValidators, boxedValidators)
                                                            .Add(paramBuilder => paramBuilder.AddDisplayName, addDisplayName)
                                                            .Add(paramBuilder => paramBuilder.OnValidationStarted, onValidationStarted)
                                                            .Add(paramBuilder => paramBuilder.OnValidationCompleted, onValidationCompleted));

    public class CurrentEditContext_OnFieldChanged()
    {
        [Fact]
        public void Should_find_the_parent_property_name_from_the_child_object_model()
        {
            var context                 = new TestContext();
            var contactData             = StaticData.CreateContactObjectGraph();
            var prebuiltAddressBuilder  = ContactModelValidators.PreBuiltAddressBuilder;
            var addressLineField        = new FieldIdentifier(contactData.Address, nameof(AddressDto.AddressLine));

            var boxedValidators = BlazorValidationBuilder<ContactDto>.Create()
                                    .ForNestedMember(c => c.Address, prebuiltAddressBuilder.GetBoxedValidators())
                                        .GetBoxedValidators();

            var editContext       = new EditContext(contactData);
            var renderedComponent = CreateValidatorComponent(context, editContext, boxedValidators);

            contactData.Address.AddressLine = "1"; //fails validation

            editContext.NotifyFieldChanged(addressLineField);

            var failureMessages = editContext.GetValidationMessages(addressLineField).ToList();

            failureMessages.Count.Should().Be(1);

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_prefix_failure_message_with_the_display_name_if_the_add_display_name_parameter_is_true(bool addDisplayName)
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            var boxedValidators = BlazorValidationBuilder<ContactDto>.Create().ForMember(c => c.Title, ContactModelValidators.TitleValidator);
            _ = CreateValidatorComponent(context, editContext, boxedValidators.GetBoxedValidators(), addDisplayName);

            contactData.Title = "D";//fails validation

            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            if(true == addDisplayName)
            {
                editContext.GetValidationMessages().ToList()[0].Should().StartWith(nameof(contactData.Title));
                return;
            }
            editContext.GetValidationMessages().ToList()[0].Should().NotStartWith(nameof(contactData.Title));
        }
    }


    public class CurrentEditContext_OnValidationRequested
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_prefix_failure_message_with_the_display_name_if_the_add_display_name_parameter_is_true(bool addDisplayName)
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);
                        
            var boxedValidators = BlazorValidationBuilder<ContactDto>.Create()
                                    .ForMember(c => c.Title, ContactModelValidators.TitleValidator)
                                        .ForNestedMember(c => c.ContactMethods, ContactModelValidators.PreBuiltContactMethodBuilder.GetBoxedValidators());

            _ = CreateValidatorComponent(context, editContext, boxedValidators.GetBoxedValidators(), addDisplayName);

            contactData.Title = "D";//fails validation
            contactData.ContactMethods = [new("MethodType", MethodValue: "")];//method value fails validation

            _ = editContext.Validate();

            if (true == addDisplayName)
            {
                var failureMessages = editContext.GetValidationMessages().ToList();
                failureMessages[0].Should().StartWith(nameof(ContactDto.Title));
                failureMessages[1].Should().StartWith("Method value");
                return;
            }
            var errorMessages = editContext.GetValidationMessages().ToList();
            errorMessages[0].Should().NotStartWith(nameof(ContactDto.Title));
            errorMessages[1].Should().NotStartWith("Method value");
        }
    }
}
