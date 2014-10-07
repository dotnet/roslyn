using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonConversionExtensions
    {
        public static bool IsIdentityOrImplicitReference(this CommonConversion conversion)
        {
            return conversion.IsIdentity ||
                (conversion.IsImplicit && conversion.IsReference);
        }

        public static bool IsImplicitUserDefinedConversion(this CommonConversion conversion)
        {
            return conversion.IsUserDefined &&
                conversion.Method != null &&
                conversion.Method.MethodKind == CommonMethodKind.Conversion &&
                conversion.Method.Name == "op_Implicit";
        }
    }
}