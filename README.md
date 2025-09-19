[![.NET](https://github.com/code-dispenser/Validated-Blazor/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/code-dispenser/Validated-Blazor/actions/workflows/dotnet.yml) 
[![Coverage Status](https://coveralls.io/repos/github/code-dispenser/Validated-Blazor/badge.svg?branch=main)](https://coveralls.io/github/code-dispenser/Validated-Blazor?branch=main)
<h1>
<img src="https://raw.github.com/code-dispenser/Validated/main/assets/logo-64.png" align="center" alt="Validated icon" /> Validated.Blazor
</h1>
<!--
# ![icon](https://raw.githubusercontent.com/code-dispenser/validated/main/assets/logo-64.png) Validated.Blazor
-->

## Overview

Validated.Blazor is the official UI component library for the [Validated.Core](https://github.com/code-dispenser/Validated) validation framework. It brings the power, flexibility, and composability of functional validation directly into your Blazor applications, providing a seamless and robust alternative to standard DataAnnotations.

At its heart, Validated.Blazor is designed to bridge the declarative, configuration-driven logic of Validated.Core with Blazor's reactive EditContext system. This allows you to build complex, dynamic, and maintainable forms with validation logic that can be shared, versioned, and customized for multi-tenant environments.

**Full documentation available at:** https://code-dispenser.gitbook.io/validated-blazor-docs/

## Key Features

- Native Blazor Integration: Hooks directly into Blazor's `<EditForm>` and `EditContext`. It uses a standard `ValidationMessageStore` to display errors, making it compatible out-of-the-box with Blazor's built-in `<ValidationMessage>` and `<ValidationSummary>` components.
- Reuses Core Logic: **Validated.Blazor** is a lightweight presentation layer. It consumes the same `MemberValidator<T>` delegates and `IValidatorFactoryProvider` from **Validated.Core**, meaning all your existing validation rules, factories, and configurations can be used without modification.
- Specialized Blazor Builders: Includes `BlazorValidationBuilder<TEntity>` and `BlazorTenantValidationBuilder<TEntity>` to assemble your validation rules. Instead of producing a single delegate, these builders create a dictionary of validators that the main component consumes, perfectly adapting the core pattern for a component-based UI.
- Complex Model Support: Natively understands and traverses complex object graphs. It effortlessly handles validation for nested objects and collections of objects, ensuring that validation messages are correctly mapped to the right fields, no matter how deep your model is.

## Getting Started
Add the Validated.Core nuget package to your project using Nuget Package Manager or the dotnet CLI:
```
dotnet add package Validated.Blazor
```
## Usage
**Please see the Demo project** included int the solution for usage. Extract below taken from one of the example pages.

**Nb.** The demo only uses the default Blazor templates and associated files

For more information on the actual validators and/or creating validators please see the [Validated.Core](https://github.com/code-dispenser/Validated)  library repo and associated [documentation](https://code-dispenser.gitbook.io/validated-docs/).

```razor
<EditForm EditContext="_editContext">
    <BlazorValidated TEntity="ContactDto" BoxedValidators="_contactBoxedValidators" AddDisplayName="true" OnValidationStarted="OnValidationStarted" OnValidationCompleted="OnValidationCompletedStarted" />
    <ValidationSummary />

    <div class="row mb-2">
        <div class="col-sm-6">
            <label class="form-label" for="textBoxTitle">Title:</label>
            <InputText id="textBoxTitle" Class="form-control" @bind-Value="_contactData.Title" />
            <ValidationMessage For="() => _contactData.Title" />
        </div>
    </div>
    <div class="row mb-2">
        <div class="col-sm-6">
            <label class="form-label" for="textBoxFirstName">First name:</label>
            <InputText id="textBoxFirstName" Class="form-control" @bind-value="_contactData.GivenName" />
            <ValidationMessage For="() => _contactData.GivenName" />
        </div>
        <div class="col-sm-6">
            <label class="form-label" for="textBoxSurname">Surname:</label>
            <InputText id="textBoxSurname" Class="form-control" @bind-Value="_contactData.FamilyName" />
            <ValidationMessage For="() => _contactData.FamilyName" />
        </div>
    </div>
    <div class="row mb-2">
        <div class="col-sm-6">
            <label class="form-label" for="textBoxAge">Age:</label>
            <InputNumber id="textBoxAge" Class="form-control" @bind-value="_contactData.Age" />
            <ValidationMessage For="() => _contactData.Age" />
        </div>
    </div>
    <div class="row mb-2">
        <div class="col-sm-6">
            <label class="form-label" for="textBoxDOB">Date of birth:</label>
            <InputDate id="textBoxDOB" Class="form-control" @bind-value="_contactData.DOB" />
            <ValidationMessage For="() => _contactData.DOB" />
        </div>
        <div class="col-sm-6">
            <label class="form-label" for="textBoxCompareDOB">Compare DOB:</label>
            <InputDate id="textBoxCompareDOB" Class="form-control" @bind-Value="_contactData.CompareDOB" />
            <ValidationMessage For="() => _contactData.CompareDOB" />
        </div>
    </div>
   
    <div class="row mb-2">
        <h5 class="col-sm-12 mb-2 mt-2">Address (nested complex type):</h5>
        <div class="col-sm-12">
            <label class="form-label" for="textBoxAddressLine">Address line:</label>
            <InputText id="textBoxAddressLine" Class="form-control" @bind-value="_contactData.Address.AddressLine" />
            <ValidationMessage For="() => _contactData.Address.AddressLine" />
        </div>
    </div>
    <div class="row mb-2">
        <div class="col-sm-4">
            <label class="form-label" for="textBoxTownCity">Town / city:</label>
            <InputText id="textBoxTownCity" Class="form-control" @bind-value="_contactData.Address.TownCity" />
            <ValidationMessage For="() => _contactData.Address.TownCity" />
        </div>
        <div class="col-sm-4">
            <label class="form-label" for="textBoxCounty">County:</label>
            <InputText id="textBoxCounty" Class="form-control" @bind-Value="_contactData.Address.County" />
            <ValidationMessage For="() => _contactData.Address.County" />
        </div>
        <div class="col-sm-4">
            <label class="form-label" for="textBoxPostCode">Postcode:</label>
            <InputText id="textBoxPostCode" Class="form-control" @bind-Value="_contactData.Address.NullablePostcode" />
            <ValidationMessage For="() => _contactData.Address.NullablePostcode" />
        </div>
    </div>

    <div class="row mb-2">
        <h5 class="col-sm-12 mb-2 mt-2">Contact methods (collection of complex types):</h5>
        @{
            var index = 0;
        }
        @foreach (var contactMethod in _contactData.ContactMethods)
        {
            <div class="col-sm-6">
                <label class="form-label" for=@("texBoxMethodType-" + index)>Method type:</label>
                <InputText id=@("texBoxMethodType-" + index) Class="form-control" @bind-Value="@contactMethod.MethodType" />
                <ValidationMessage For="() => contactMethod.MethodType" />
            </div>
            <div class="col-sm-6">
                <label class="form-label" for=@("texBoxMethodValue-" + index)>Method value:</label>
                <InputText id="@("texBoxMethodValue=" + index)" Class="form-control" @bind-Value="@contactMethod.MethodValue" />
                <ValidationMessage For="() => contactMethod.MethodValue" />
            </div>
            <div class="col-sm-12">
                <button class="btn btn-danger" type="button" @onclick="() => RemoveContactMethod(contactMethod)">Delete</button>
                <hr />
            </div>

            index++;
        }

    </div>

    <button class="btn btn-primary" type="submit">Submit</button>
    <button class="btn btn-primary" type="button" @onclick="AddContactMethod">Add Contact Method</button>

</EditForm>



@code {
    private EditContext _editContext = default!;
    private ContactDto _contactData  = StaticData.CreateContactObjectGraph();

    private ImmutableDictionary<string, BoxedValidator> _contactBoxedValidators = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(_contactData);
        /*
        * Just using one builder here so you can see things together. You just compose want you want and share what you want. 
        */
        var builder = BlazorValidationBuilder<ContactDto>.Create()
                                .ForMember(c => c.Title, ContactValidators.TitleValidator)                      //< using a ready built title member validator
                                .ForMember(c => c.GivenName, ContactValidators.GivenNameValidator)
                                .ForMember(c => c.FamilyName, ContactValidators.FamilyNameValidator)            // << This validator (function) combines two functions a regex and a separate length validator
                                .ForMember(c => c.Age, ContactValidators.AgeValidator)
                                .ForComparisonWithValue(c => c.DOB, ContactValidators.DOBValidator)
                                .ForComparisonWithMember(c => c.CompareDOB, ContactValidators.CompareDOBValidator)
                                .ForNestedMember(c => c.Address, AddressValidators.GetBoxedAddressValidators())// << Getting all the address validators from a pre-created builder.
                                .ForEachCollectionMember(c => c.ContactMethods, ContactMethodValidators.GetBoxedContactMethodValidators())
                                .ForCollection(c => c.ContactMethods, MemberValidators.CreateCollectionLengthValidator<List<ContactMethodDto>>(1, 3, "ContactMethods", "Contact methods", "Must have at least 1 contact method but no more than 3"));// << created on the fly


        _contactBoxedValidators = builder.GetBoxedValidators();

        _contactData.ContactMethods = [];

    }

    private void AddContactMethod() 

        => _contactData.ContactMethods.Add(new ContactMethodDto());

    private void RemoveContactMethod(ContactMethodDto contactMethod)

        => _contactData.ContactMethods.Remove(contactMethod);

    private async Task<CancellationToken> OnValidationStarted(ValidationLevel validationLevel, FieldIdentifier? fieldIdentifier)
    {
        return await Task.FromResult(CancellationToken.None);
    }
    private async Task OnValidationCompletedStarted(ValidationLevel validationLevel, FieldIdentifier? fieldIdentifier, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

}


```
**Full documentation available at:** https://code-dispenser.gitbook.io/validated-blazor-docs/