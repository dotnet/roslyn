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
                { IsValueType: true } => false,
                ITypeParameterSymbol typeParameter => typeParameter.IsRefernceTypeParameter(),
                _ => true,
            };

        public static bool IsRefernceTypeParameter(this ITypeParameterSymbol typeParameter)
            => typeParameter switch
            {
                { HasValueTypeConstraint: true } => false,
                { HasReferenceTypeConstraint: true } => true,
                { ConstraintTypes: var constrainedTypes } => constrainedTypes.Any(t => t switch
                    {
                        { TypeKind: TypeKind.Class } => true,
                        ITypeParameterSymbol nested => nested.IsRefernceTypeParameter(),
                        _ => false,
                    }),
                _ => false,
            };
    }
}
