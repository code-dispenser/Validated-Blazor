using Validated.Blazor.Builders;
using Validated.Blazor.Tests.SharedDataFixtures.Common.Models;
using Validated.Core.Common.Constants;
using Validated.Core.Extensions;
using Validated.Core.Types;
using Validated.Core.Validators;

namespace Validated.Blazor.Tests.SharedDataFixtures.Common.Validators;

public static class ContactModelValidators
{
    public static MemberValidator<string> TitleValidator        { get; }
    public static MemberValidator<string> GivenNameValidator    { get; }
    public static MemberValidator<string> FamilyNameValidator   { get; }
    public static MemberValidator<int>    AgeValidator          { get; }
    public static MemberValidator<int>    NullableAgeValidator  { get; }
    public static MemberValidator<string> MobileValidator       { get; }
    public static MemberValidator<string> EntryValidator        { get; }
    public static MemberValidator<string> MethodValueValidator  { get; }
    public static MemberValidator<string> MethodTypeValidator   { get; }
    public static MemberValidator<List<string>> EntryCountValidator { get; }

    public static MemberValidator<ContactDto> CompareDOBValidator { get; }
    public static MemberValidator<DateOnly>   DOBValidator        { get; }

    public static MemberValidator<string> AddressLineValidator  { get; }
    public static MemberValidator<string> TownCityValidator     { get; }
    public static MemberValidator<string> CountyValidator       { get; }
    public static MemberValidator<string> UKPostcodeValidator   { get; }


    public static BlazorValidationBuilder<AddressDto> PreBuiltAddressBuilder { get; }

    public static BlazorValidationBuilder<ContactMethodDto> PreBuiltContactMethodBuilder { get; }

    static ContactModelValidators()
    {
        TitleValidator       = MemberValidators.CreateStringRegexValidator("^(Mr|Mrs|Ms|Dr|Prof)$", "Title", "Title", "Must be one of Mr, Mrs, Ms, Dr, Prof");

        GivenNameValidator   = MemberValidators.CreateStringRegexValidator(@"^(?=.{2,50}$)[A-Z]+['\- ]?[A-Za-z]*['\- ]?[A-Za-z]+$", "GivenName", "First name", "Must start with a capital letter and be between 2 and 50 characters in length");

        FamilyNameValidator  = MemberValidators.CreateStringRegexValidator(@"^[A-Z]+['\- ]?[A-Za-z]*['\- ]?[A-Za-z]*$", "FamilyName", "Surname", "Must start with a capital letter")
                                .AndThen(MemberValidators.CreateStringLengthValidator(2, 50, "FamilyName", "Surname", "Must be between 2 and 50 characters in length"));

        AgeValidator         = MemberValidators.CreateRangeValidator(10, 50, "Age", "Age", "Must be between 10 and 50");

        NullableAgeValidator = MemberValidators.CreateRangeValidator(10, 50, "NullableAge", "NullableAge", "Must be between 10 and 50");

        MobileValidator      = MemberValidators.CreateStringRegexValidator(@"^(?:\+[1-9]\d{1,3}[ -]?7\d{9}|07\d{9})$", "Mobile", "Mobile Tel", "Must be a valid UK mobile number format");

        EntryValidator       = MemberValidators.CreateNotNullOrEmptyValidator<string>("Entry", "Entry", "Required, cannot be missing, null or empty");

        MethodValueValidator = MemberValidators.CreateNotNullOrEmptyValidator<string>("MethodValue", "Method value", "Required, cannot be missing, null or empty");

        MethodTypeValidator  = MemberValidators.CreateNotNullOrEmptyValidator<string>("MethodType", "Method type", "Required, cannot be missing, null or empty");


        EntryCountValidator = MemberValidators.CreateCollectionLengthValidator<List<string>>(1, 3, "Entries", "Entries", $"Must contain between 1 and 3 items but the collection contained {FailureMessageTokens.ACTUAL_LENGTH} items");


        AddressLineValidator = MemberValidators.CreateStringRegexValidator(@"^(?=.{5,250}$)(?!.* {2})(?!.*[,\-']{2})[A-Za-z0-9][A-Za-z0-9 ,\-\n']+[A-Za-z0-9]$", "AddressLine",
                                                                          "Address Line", "Must start with a letter or number and be 5 to 250 characters in length.");

        TownCityValidator   = MemberValidators.CreateStringRegexValidator(@"^(?=.{3,100}$)[A-Z](?!.* {2})(?!.*'{2})(?!.*-{2})[\-A-Za-z ']+[a-z]+$", "TownCity",
                                                                        "Town / City", "Must start with a capital letter and be between 3 to 100 characters in length.");

        CountyValidator     = MemberValidators.CreateStringRegexValidator(@"^(?=.{3,100}$)[A-Z](?!.* {2})(?!.*'{2})(?!.*-{2})[\-A-Za-z ']+[a-z]+$", "County",
                                                                      "County", "Must start with a capital letter and be between 3 to 100 characters in length.");

        UKPostcodeValidator = MemberValidators.CreateStringRegexValidator(@"^(GIR 0AA)|((([ABCDEFGHIJKLMNOPRSTUWYZ][0-9][0-9]?)|(([ABCDEFGHIJKLMNOPRSTUWYZ][ABCDEFGHKLMNOPQRSTUVWXY][0-9][0-9]?)|(([ABCDEFGHIJKLMNOPRSTUWYZ][0-9][ABCDEFGHJKSTUW])|([ABCDEFGHIJKLMNOPRSTUWYZ][ABCDEFGHKLMNOPQRSTUVWXY][0-9][ABEHMNPRVWXY])))) [0-9][ABDEFGHJLNPQRSTUWXYZ]{2})$",
                                                                          "Postcode", "Postcode", "Must be a valid UK formatted postcode.");


        CompareDOBValidator = MemberValidators.CreateMemberComparisonValidator<ContactDto, DateOnly>(c => c.CompareDOB, c => c.DOB, CompareType.GreaterThan, "Compare Date of birth", "Must be greater than  Date of birth");


        DOBValidator = MemberValidators.CreateCompareToValidator<DateOnly>(DateOnly.Parse("2022-01-01"), CompareType.EqualTo, "DOB", "Date of birth", "Must be equal to 2022-01-01");



        PreBuiltAddressBuilder = BlazorValidationBuilder<AddressDto>.Create()
                                        .ForMember(a => a.AddressLine, AddressLineValidator)
                                        .ForMember(a => a.TownCity, TownCityValidator)
                                        .ForMember(a => a.County, CountyValidator)
                                        .ForNullableStringMember(a => a.Postcode, UKPostcodeValidator);


        PreBuiltContactMethodBuilder = BlazorValidationBuilder<ContactMethodDto>.Create()
                                            .ForMember(c => c.MethodValue, MethodValueValidator)
                                                .ForMember(c => c.MethodType, MethodTypeValidator);
    }




}