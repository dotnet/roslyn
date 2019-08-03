// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            if (typeSymbol is TypeSymbolWithNullableAnnotation typeSymbolWithNullability)
            {
                // Check if the wrapper already has the same top-level nullability; in that case we don't need to re-create one.
                if (typeSymbolWithNullability.Nullability == nullability)
                {
                    return typeSymbol;
                }

                // No reason to wrap a wrapper, so unwrap it
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
                case NullableFlowState.None:
                    return typeSymbol.WithNullability(NullableAnnotation.None);
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
                return NullableAnnotation.None;
            }
        }

        public static T WithoutNullability<T>(this T typeSymbol) where T : INamespaceOrTypeSymbol
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

        public static ITypeSymbol GetConvertedTypeWithFlowNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullability(typeInfo.Nullability.FlowState);

        public static ITypeSymbol GetConvertedTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullability(typeInfo.Nullability.Annotation);

        public static ITypeSymbol GetTypeWithFlowNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullability(typeInfo.Nullability.FlowState);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullability(typeInfo.Nullability.Annotation);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this IFieldSymbol fieldSymbol)
            => fieldSymbol.Type.WithNullability(fieldSymbol.NullableAnnotation);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this IParameterSymbol parameterSymbol)
            => parameterSymbol.Type.WithNullability(parameterSymbol.NullableAnnotation);

        public static ITypeSymbol GetElementTypeWithAnnotatedNullability(this IArrayTypeSymbol arrayTypeSymbol)
            => arrayTypeSymbol.ElementType.WithNullability(arrayTypeSymbol.ElementNullableAnnotation);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this IPropertySymbol propertySymbol)
            => propertySymbol.Type.WithNullability(propertySymbol.NullableAnnotation);

        public static ITypeSymbol GetReturnTypeWithAnnotatedNullability(this IMethodSymbol methodSymbol)
            => methodSymbol.ReturnType.WithNullability(methodSymbol.ReturnNullableAnnotation);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this IEventSymbol eventSymbol)
            => eventSymbol.Type.WithNullability(eventSymbol.NullableAnnotation);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this ILocalSymbol localSymbol)
            => localSymbol.Type.WithNullability(localSymbol.NullableAnnotation);

        public static INamedTypeSymbol ConstructWithNullability(this INamedTypeSymbol typeSymbol, params ITypeSymbol[] typeArguments)
        {
            return typeSymbol.Construct(typeArguments.SelectAsArray(t => t.WithoutNullability()), typeArguments.SelectAsArray(t => t.GetNullability()));
        }
    }
}
