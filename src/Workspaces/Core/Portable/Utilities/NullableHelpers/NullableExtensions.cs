// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        public static T WithNullability<T>(this T typeSymbol, NullableFlowState flowState) where T : class, ITypeSymbol
        {
            // TODO: call the compiler API once it's available
            switch (flowState)
            {
                case NullableFlowState.None:
                    return (T)typeSymbol.WithNullableAnnotation(NullableAnnotation.None);
                case NullableFlowState.NotNull:
                    return (T)typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                case NullableFlowState.MaybeNull:
                    return (T)typeSymbol.WithNullableAnnotation(NullableAnnotation.Annotated);
                default:
                    throw ExceptionUtilities.UnexpectedValue(typeSymbol);
            }
        }

        public static ITypeSymbol? GetConvertedTypeWithFlowNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullability(typeInfo.ConvertedNullability.FlowState);

        public static ITypeSymbol? GetConvertedTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullableAnnotation(typeInfo.ConvertedNullability.Annotation);

        public static ITypeSymbol? GetTypeWithFlowNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullability(typeInfo.Nullability.FlowState);

        public static ITypeSymbol? GetTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullableAnnotation(typeInfo.Nullability.Annotation);
    }
}
