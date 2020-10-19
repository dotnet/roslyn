using System.Linq;

#nullable enable

namespace Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators
{
    internal static class ITypeSymbolExtensions
    {
        public static bool IsReferenceTypeOrTypeParameter(this ITypeSymbol? type)
            => type switch
            {
                null => false,
                { Kind: SymbolKind.ErrorType } => false,
                { IsReferenceType: true } => true,
                _ => false,
            };
    }
}
