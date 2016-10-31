using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IConversionExtensions
    {
        public static bool IsIdentityOrWideningReference<TConversion>(this TConversion conversion)
            where TConversion : IConversion
        {
            return conversion.IsIdentity ||
                (conversion.IsWidening && conversion.IsReference);
        }
    }
}