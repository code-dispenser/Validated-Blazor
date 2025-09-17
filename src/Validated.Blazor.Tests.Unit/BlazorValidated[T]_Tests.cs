using Bunit;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Immutable;
using System.ComponentModel;
using Validated.Blazor.Common.Constants;
using Validated.Blazor.Common.Types;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Data;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;


namespace Validated.Blazor.Tests.Unit;

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
    public class OnInitialized 
    {
        [Fact]
        public void Should_thrown_an_invalid_operation_exception_if_the_edit_context_is_null()
        {
            using var context = new TestContext();

            FluentActions.Invoking(() => context.RenderComponent<BlazorValidated<ContactDto>>())
                                .Should().ThrowExactly<InvalidOperationException>().WithMessage(ErrorMessages.Validator_Missing_Context_Message);
        }
        [Fact]
        public void Should_thrown_an_invalid_operation_exception_if_the_boxed_validators_param_is_null()
        {
            using var context = new TestContext();

            var editContext = new EditContext(StaticData.CreateContactObjectGraph());

            FluentActions.Invoking(() => context.RenderComponent<BlazorValidated<ContactDto>>(paramBuilder => paramBuilder.AddCascadingValue(editContext)))
                             .Should().ThrowExactly<InvalidOperationException>().WithMessage(ErrorMessages.Validator_Missing_Boxed_Validators_Message);
        }

        [Fact]
        public void Should_thrown_an_invalid_operation_exception_if_the_boxed_validators_param_is_empty()
        {
            using var context = new TestContext();

            var editContext = new EditContext(StaticData.CreateContactObjectGraph());

            ImmutableDictionary<string,BoxedValidator> boxedValidators = ImmutableDictionary<string ,BoxedValidator>.Empty;

            FluentActions.Invoking(() => context.RenderComponent<BlazorValidated<ContactDto>>(paramBuilder => paramBuilder.AddCascadingValue(editContext)
                                                                                                                          .Add(paramBuilder => paramBuilder.BoxedValidators, boxedValidators)))
                             .Should().ThrowExactly<InvalidOperationException>().WithMessage(ErrorMessages.Validator_Missing_Boxed_Validators_Message);
        }

        [Fact]
        public void Should_initialise_correctly_with_valid_parameters()
        {
            using var context = new TestContext();

            var editContext = new EditContext(StaticData.CreateContactObjectGraph());

            ImmutableDictionary<string, BoxedValidator> boxedValidators = BoxedValidators.OnlyTheContactTitleValidator();

            var validatorComponent = context.RenderComponent<BlazorValidated<ContactDto>>(paramBuilder => paramBuilder.AddCascadingValue(editContext)
                                                                                                                            .Add(paramBuilder => paramBuilder.BoxedValidators, boxedValidators));

            validatorComponent.Should().NotBeNull();
        }
    }


    public class CurrentEditContext_OnFieldChanged
    {
        [Fact]
        public void Should_validate_field_when_the_field_changes()
        {
            var context            = new TestContext();    
            var contactData        = StaticData.CreateContactObjectGraph();
            var editContext        = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator());

            contactData.Title = "D";//fails validation

            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.Title))).Should().NotBeEmpty();
        }

        [Fact]
        public void Should_clear_validation_messages_for_a_valid_field()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator());

            contactData.Title = "D";//invalid value

            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            var failedMessages = editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.Title))).ToList();

            contactData.Title = "Dr";//valid value
            
            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            var messages = editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.Title))).ToList();

            using (new AssertionScope())
            {
                failedMessages.Count.Should().Be(1);
                messages.Count.Should().Be(0);
            }
        }

        [Fact]
        public void No_validation_Should_occur_or_errors_raised_if_there_is_no_validator()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactGivenNameValidator());

            contactData.Title = "D";//invalid value

            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.Title))).Should().BeEmpty();
        }

        [Fact]
        public void Should_be_able_to_receive_the_validation_start_call_back_if_a_call_back_func_was_provided()
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            ValidationLevel level     = ValidationLevel.Model;
            string          fieldName = string.Empty;

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator(), false,
                (validationLevel, fieldIdentifier) => 
                {
                    fieldName = fieldIdentifier.HasValue ? fieldIdentifier.Value.FieldName : String.Empty;
                    level     = validationLevel;
                    return Task.FromResult(CancellationToken.None);
                },null);

            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            using(new AssertionScope())
            {
                level.Should().Be(ValidationLevel.Field);
                fieldName.Should().Be(nameof(ContactDto.Title));
            }
        }

        [Fact]
        public void Should_not_propagate_exceptions_occurring_in_the_users_on_validation_started_call_back()
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator(), false,
                (validationLevel, fieldIdentifier) =>
                {
                    throw new InvalidOperationException();
                   
                }, null);

            FluentActions.Invoking(() => editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title))))
                            .Should().NotThrow();
        }

        [Fact]
        public void Should_be_able_to_receive_the_validation_completed_call_back_if_a_call_back_func_was_provided()
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);
            
            ValidationLevel level     = ValidationLevel.Model;
            string          fieldName = string.Empty;

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator(), false,null,
                (validationLevel, fieldIdentifier, _) =>
                {
                    fieldName = fieldIdentifier.HasValue ? fieldIdentifier.Value.FieldName : String.Empty;
                    level     = validationLevel;
                    return Task.CompletedTask;
                } );

            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            using (new AssertionScope())
            {
                level.Should().Be(ValidationLevel.Field);
                fieldName.Should().Be(nameof(ContactDto.Title));
            }
        }
    }

    public class CurrentEditContext_OnValidationRequested()
    {
        [Fact]
        public void Should_validate_the_entire_form_model_on_validation_requested_returning_false_if_there_is_an_invalid_entry()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.TitleAndNestedAddressLineValidators());

            contactData.Title = "D";//invalid
            contactData.Address.AddressLine = "1";//invalid

            var isValid  = editContext.Validate();
            var messages = editContext.GetValidationMessages().ToList();
            
            using(new AssertionScope())
            {
                isValid.Should().BeFalse();
                messages.Count.Should().Be(2);
            }
        }
        [Fact]
        public void Should_validate_the_entire_form_model_on_validation_requested_returning_true_if_there_are_no_failures()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.TitleAndNestedAddressLineValidators());

            var isValid = editContext.Validate();

            using (new AssertionScope())
            {
                isValid.Should().BeTrue();
                editContext.GetValidationMessages().ToList().Count.Should().Be(0);
            }
        }
        [Fact]
        public void Should_be_able_to_receive_the_validation_start_call_back_if_a_call_back_func_was_provided()
        {
            var context = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            ValidationLevel level = ValidationLevel.Field;
            string fieldName = string.Empty;

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator(), false,
                (validationLevel, fieldIdentifier) =>
                {
                    fieldName = fieldIdentifier.HasValue ? fieldIdentifier.Value.FieldName : String.Empty;
                    level     = validationLevel;
                    return Task.FromResult(CancellationToken.None);
                }, null);

            _ = editContext.Validate();

            using (new AssertionScope())
            {
                level.Should().Be(ValidationLevel.Model);
                fieldName.Should().Be(String.Empty);
            }
        }

        [Fact]
        public void Should_not_propagate_exceptions_occurring_in_the_users_on_validation_started_call_back()
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator(), false,
                (validationLevel, FieldIdentifier) =>
                {
                    throw new InvalidOperationException();

                },null);

            FluentActions.Invoking(() => editContext.Validate())
                            .Should().NotThrow();
        }


        [Fact]
        public void Should_be_able_to_receive_the_validation_completed_call_back_if_a_call_back_func_was_provided()
        {
            var context = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            ValidationLevel level = ValidationLevel.Field;
            string fieldName = string.Empty;

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator(), false, null,
                (validationLevel, fieldIdentifier, _) =>
                {
                    fieldName = fieldIdentifier.HasValue ? fieldIdentifier.Value.FieldName : String.Empty;
                    level     = validationLevel;
                    return Task.CompletedTask;
                });

            _ = editContext.Validate();

            using (new AssertionScope())
            {
                level.Should().Be(ValidationLevel.Model);
                fieldName.Should().Be(String.Empty);
            }
        }
    }

    public class ValidatedField()
    {
        [Fact]
        public void Should_pass_validation_for_null_values_when_the_member_is_optional_nullable()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactNullableAgeValidator());

            contactData.NullableAge = 1;//invalid age
            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.NullableAge)));
            
            var errorMessages = editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.NullableAge))).ToList();

            contactData.NullableAge = null;//optional age
            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.NullableAge)));

            var messages = editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.NullableAge))).ToList();

            using (new AssertionScope())
            {
                errorMessages.Count.Should().Be(1);
                messages.Count.Should().Be(0);
            }
        }
        [Fact]
        public void Should_fail_validation_for_null_values_when_the_member_is_not_optional_nullable()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactTitleValidator());

            contactData.Title = null!;
            editContext.NotifyFieldChanged(new FieldIdentifier(contactData, nameof(ContactDto.Title)));

            editContext.GetValidationMessages(new FieldIdentifier(contactData, nameof(ContactDto.Title))).ToList()
                            .Count.Should().Be(1);    

        }

    }

    public class ValidatedModel()
    {
        [Fact]
        public void Should_be_able_to_validate_at_the_collection_level_such_as_using_count()
        {
            var context           = new TestContext();
            var contactData       = StaticData.CreateContactObjectGraph();
            var editContext       = new EditContext(contactData);
            
            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactEntriesValidator());

            contactData.Entries = ["Entry1","Entry2","Entry3","Entry4"]; //needs between 1 and 3 inclusive

            var isValid = editContext.Validate();

            using(new AssertionScope())
            {
                isValid.Should().BeFalse();
                editContext.GetValidationMessages().ToList().Count.Should().Be(1);
            }
        }

        [Fact]
        public void Should_not_allow_parent_child_parent_infinite_loops()
        {
            var context     = new TestContext();
            var parentChild = new Parent("Parent", "Child");//has cyclic reference
            var editContext = new EditContext(parentChild);

            context.RenderComponent<BlazorValidated<Parent>>(paramBuilder => paramBuilder.AddCascadingValue(editContext)
                                                            .Add(paramBuilder => paramBuilder.BoxedValidators, BoxedValidators.OnlyTheContactTitleValidator()));//validator not important

            editContext.Validate().Should().BeTrue();
        }

        [Fact]
        public void Collection_level_validations_with_null_collections_should_fail_validation()
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactEntriesValidator());

            contactData.Entries = null!;

            var isValid = editContext.Validate();

            using (new AssertionScope())
            {
                isValid.Should().BeFalse();
                editContext.GetValidationMessages().ToList().Count.Should().Be(1);
            }
        }


        [Fact]
        public void Collection_level_validations_that_are_nullable_should_pass_validation()//only added for potential future change
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.OnlyTheContactOptionalEntriesValidator());

            contactData.Entries = null!;

            var isValid = editContext.Validate();

            using (new AssertionScope())
            {
                isValid.Should().BeTrue();
                editContext.GetValidationMessages().ToList().Count.Should().Be(0);
            }
        }

        [Fact]
        public void Nested_non_nullable_members_should_fail_validation_if_null()
        {
            var context     = new TestContext();
            var contactData = StaticData.CreateContactObjectGraph();
            var editContext = new EditContext(contactData);

            _ = CreateValidatorComponent(context, editContext, BoxedValidators.TitleAndNestedAddressLineValidators());

            contactData.Address = null!;

            var isValid = editContext.Validate();
            using (new AssertionScope())
            {
                isValid.Should().BeFalse();
                editContext.GetValidationMessages().ToList().Count.Should().Be(1);
            }
        }

        [Fact]
        public void Unsupported_for_each_primitive_item_validators_should_be_skipped_and_pass()
        {
            var context       = new TestContext();
            var primitiveData = new PrimitiveCollectionHolder("PrimitiveHolder", [5,6]);
            var editContext   = new EditContext(primitiveData);

            context.RenderComponent<BlazorValidated<PrimitiveCollectionHolder>>(paramBuilder => paramBuilder.AddCascadingValue(editContext)
                                                            .Add(paramBuilder => paramBuilder.BoxedValidators, BoxedValidators.OnlyTheContactPrimitiveValidator()));//validator not important


        
             editContext.Validate().Should().BeTrue();
        }

    }

}

