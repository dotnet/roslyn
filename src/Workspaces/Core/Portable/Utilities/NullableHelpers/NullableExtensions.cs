using System;
using System.Collections.Generic;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        public static T WithNullability<T>(this T typeSymbol, NullableAnnotation nullability) where T : class, ITypeSymbol
        {
            if (typeSymbol == null)
            {
                return null;
            }

            // No reason to wrap a wrapper, so unwrap it
            if (typeSymbol is TypeSymbolWithNullableAnnotation typeSymbolWithNullability)
            {
                typeSymbol = (T)typeSymbolWithNullability.WrappedSymbol;
            }

            return typeSymbol switch
            {
                IArrayTypeSymbol arrayTypeSymbol => (ITypeSymbol)new ArrayTypeSymbolWithNullableAnnotation(arrayTypeSymbol, nullability),
                IDynamicTypeSymbol dynamicTypeSymbol => (ITypeSymbol)new DynamicTypeSymbolWithNullableAnnotation(dynamicTypeSymbol, nullability),
                INamedTypeSymbol namedTypeSymbol => (ITypeSymbol)new NamedTypeSymbolWithNullableAnnotation(namedTypeSymbol, nullability),
                IPointerTypeSymbol pointerType => (ITypeSymbol)new PointerTypeSymbolWithNullableAnnotation(pointerType, nullability),
                ITypeParameterSymbol typeParameterSymbol => (ITypeSymbol)new TypeParameterSymbolWithNullableAnnotation(typeParameterSymbol, nullability),
                _ => throw ExceptionUtilities.UnexpectedValue(typeSymbol)
            } as T;
        }

        public static T WithNullability<T>(this T typeSymbol, NullableFlowState flowState) where T : class, ITypeSymbol
        {
            // TODO: call the compiler API once it's available
            switch (flowState)
            {
                case NullableFlowState.NotApplicable:
                    return typeSymbol.WithNullability(NullableAnnotation.NotApplicable);
                case NullableFlowState.NotNull:
                    return typeSymbol.WithNullability(NullableAnnotation.NotAnnotated);
                case NullableFlowState.MaybeNull:
                    return typeSymbol.WithNullability(NullableAnnotation.Annotated);
                default:
                    throw ExceptionUtilities.UnexpectedValue(typeSymbol);
            }
        }

        public static NullableAnnotation GetNullability(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol is TypeSymbolWithNullableAnnotation typeSymbolWithNullability)
            {
                return typeSymbolWithNullability.Nullability;
            }
            else
            {
                // For now, we'll return this while we transition the codebase over to these helpers. Eventually this should throw, since it means somebody got a type from
                // the compiler API and didn't wrap it properly.
                return NullableAnnotation.NotApplicable;
            }
        }

        public static T UnwrapNullabilitySymbol<T>(this T typeSymbol) where T : ITypeSymbol
        {
            if (typeSymbol is TypeSymbolWithNullableAnnotation typeSymbolWithNullability)
            {
                return (T)typeSymbolWithNullability.WrappedSymbol;
            }
            else
            {
                return typeSymbol;
            }
        }
    }
}
