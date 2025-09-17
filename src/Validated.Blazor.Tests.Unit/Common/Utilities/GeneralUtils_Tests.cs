using FluentAssertions;
using System.Linq.Expressions;
using System.Reflection;
using Validated.Blazor.Common.Utilities;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Data;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Xunit.Sdk;

namespace Validated.Blazor.Tests.Unit.Common.Utilities;

public class GeneralUtils_Tests
{
    [Fact]
    public void The_get_member_name_should_return_the_member_name_for_a_valid_expression()

        => GeneralUtils.GetMemberName<ContactDto, string>(c => c.FamilyName).Should().Be("FamilyName");

    [Fact]
    public void The_get_member_name_should_return_a_fallback_value_for_an_invalid_expression()
    
        => GeneralUtils.GetMemberName<ContactDto, string>(c => c.FamilyName.ToString()).Should().StartWith("Contact");


    [Fact]
    public void Extract_member_name_should_return_member_name_for_unary_expression_with_member_operand()
    {
        Expression expr = ((Expression<Func<ContactDto, object>>)(c => (object)c.GivenName)).Body;
        GeneralUtils.ExtractMemberName(expr).Should().Be("GivenName");
    }
    [Fact]
    public void Extract_member_name_should_return_Item_for_method_call_expression_get_Item()
    {
        Expression expression = ((Expression<Func<ContactDto, string>>)(c => c.ContactMethods[0].MethodType)).Body;

        if (expression is MemberExpression memberExpression && memberExpression.Expression is MethodCallExpression methodExpression)
        {
            GeneralUtils.ExtractMemberName(methodExpression).Should().Be("Item");
        }
        else
        {
            throw new XunitException("Should not be here");
        }
    }
    [Fact]
    public void Extract_member_name_should_return_parameter_name_for_parameter_expression()
    {

        ParameterExpression expression = Expression.Parameter(typeof(ContactDto), nameof(ContactDto));
        GeneralUtils.ExtractMemberName(expression).Should().Be(nameof(ContactDto));
    }

    [Fact]
    public void Build_member_validator_key_should_concatenate_the_entity_or_parent_property_name_with_the_member_name()

        => GeneralUtils.BuildMemberValidatorKey(nameof(ContactDto), nameof(ContactDto.Title)).Should().Be(nameof(ContactDto) + "." + nameof(ContactDto.Title));

    [Fact]
    public void Build_member_validator_key_should_return_the_member_name_if_the_parent_is_null_or_empty()

         => GeneralUtils.BuildMemberValidatorKey(null!, nameof(ContactDto.Title)).Should().Be(nameof(ContactDto.Title));


    [Fact]
    public void Is_nullable_should_return_true_for_nullable_valueTypes()
    {
        var propertyInfo = typeof(ContactDto).GetProperty(nameof(ContactDto.NullableAge))!;
       
        GeneralUtils.IsNullable(propertyInfo).Should().BeTrue();
    }

    [Fact]
    public void Is_nullable_should_return_false_for_non_nullable_valueTypes()
    {
        var propertyInfo = typeof(ContactDto).GetProperty(nameof(ContactDto.Age))!;

        GeneralUtils.IsNullable(propertyInfo).Should().BeFalse();
    }

    [Fact]
    public void Is_nullable_should_return_true_for_nullable_reference_types()
    {
        var propertyInfo = typeof(ContactDto).GetProperty(nameof(ContactDto.Mobile))!;

        GeneralUtils.IsNullable(propertyInfo).Should().BeTrue();
    }

    [Fact]
    public void Is_nullable_should_return_false_for_non_nullable_reference_type()
    {
        var propertyInfo = typeof(ContactDto).GetProperty(nameof(ContactDto.GivenName))!;

        GeneralUtils.IsNullable(propertyInfo).Should().BeFalse();
    }

    [Fact]
    public void Is_nullable_should_return_true_for_nullable_complex_types()
    {
        var propertyInfo = typeof(ContactDto).GetProperty(nameof(ContactDto.NullableAddress))!;

        GeneralUtils.IsNullable(propertyInfo).Should().BeTrue();
    }

    [Fact]
    public void Is_nullable_should_return_false_for_non_nullable_complex_types()
    {
        var propertyInfo = typeof(ContactDto).GetProperty(nameof(ContactDto.Address))!;

        GeneralUtils.IsNullable(propertyInfo).Should().BeFalse();
    }


    [Fact]
    public void Find_model_property_name_should_return_the_parent_property_name_for_the_child_object()
    {
        var contactData = StaticData.CreateContactObjectGraph();
        var addressDto  = contactData.Address;

        GeneralUtils.FindModelPropertyName(contactData, addressDto, new HashSet<object>(ReferenceEqualityComparer.Instance))
                        .Should().Be("Address");
    }
    [Fact]
    public void Find_model_property_name_should_return_an_empty_string_if_the_current_is_null()
    {
        var contactData = StaticData.CreateContactObjectGraph();
        var addressDto = contactData.Address;

        GeneralUtils.FindModelPropertyName(null!, addressDto, new HashSet<object>(ReferenceEqualityComparer.Instance))
                        .Should().Be("");
    }

    [Fact]
    public void Find_model_property_name_should_continue_searching_if_property_values_are_null()
    {
        var contactData = StaticData.CreateContactObjectGraph();

        GeneralUtils.FindModelPropertyName(contactData, null!, new HashSet<object>(ReferenceEqualityComparer.Instance))
                        .Should().Be("");
    }

    [Fact]
    public void Find_model_property_name_should_skip_indexer_properties_when_searching()
    {

        var objectWithIndexer = new ObjectWithIndexer();
        var targetChild       = new SimpleChild { Name = "Target" };
        
        objectWithIndexer.RegularProperty = targetChild;

        var result = GeneralUtils.FindModelPropertyName(objectWithIndexer, targetChild, new HashSet<object>(ReferenceEqualityComparer.Instance));

        result.Should().Be("RegularProperty");
    }

    [Fact]
    public void Find_model_property_name_should_find_property_names_from_nested_objects()
    {
        var contactData = StaticData.CreateContactObjectGraph();
        var result      = GeneralUtils.FindModelPropertyName(contactData,contactData.Address.TownCity, new HashSet<object>(ReferenceEqualityComparer.Instance));
        result.Should().Be(nameof(ContactDto.Address));
    }

    [Fact]
    public void Find_model_property_name_should_find_names_from_collections()
    {
        var contactData = StaticData.CreateContactObjectGraph();
        var result      = GeneralUtils.FindModelPropertyName(contactData, contactData.ContactMethods[0], new HashSet<object>(ReferenceEqualityComparer.Instance));
        result.Should().Be(nameof(ContactDto.ContactMethods));
    }

    [Fact]

    public void Find_model_property_name_should_find_names_from_nested_collection_items()
    {
        var targetManager = new { Name = "John Smith", EmployeeId = 123 };
        var department    = new { Name = "IT", Managers = new[] { targetManager } };
        var organisation  = new { Name = "ACME Corp", Departments = new[] { department } };
        var visited       = new HashSet<object>(ReferenceEqualityComparer.Instance);

        var result = GeneralUtils.FindModelPropertyName(organisation, targetManager, visited);

        result.Should().Be("Departments");
    }

}


