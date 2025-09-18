using Validated.Blazor.Types;

namespace Validated.Blazor.Common.Constants;

internal static class ErrorMessages
{
    /*
        * Constants with User in them are seen by end users. 
    */

    public const string Validator_Nesting_Unsupported_Message = "Deeply nested member access is not supported in {0}. The expression {1} reaches through multiple objects. " +
                                                                       "Please use 'ForNestedMember' to validate the nested property with a dedicated validator for the nested type.";

    public const string Validator_Entity_Null_User_Message = "System validation error, {0} cannot be null. If this persists please contact support.";

    public const string Validator_Missing_Context_Message = "Missing the EditContext cascading parameter";

    public const string Validator_Missing_Boxed_Validators_Message = "The BoxedValidators parameter is required and cannot be empty.";
}