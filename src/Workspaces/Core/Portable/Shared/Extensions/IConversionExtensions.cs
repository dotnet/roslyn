using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IConversionExtensions
    {
        public static bool IsIdentityOrImplicitReference<TConversion>(this TConversion conversion)
            where TConversion : IConversion
        {
            return conversion.IsIdentity ||
                (conversion.IsImplicit && conversion.IsReference);
        }
    }
}