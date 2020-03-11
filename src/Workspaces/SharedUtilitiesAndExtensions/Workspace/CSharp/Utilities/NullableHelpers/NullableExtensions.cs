// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        public static ITypeSymbol? GetConvertedTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType
#if !CODE_STYLE // TODO: Remove the #if once WithNullableAnnotation is available.
                ?.WithNullableAnnotation(typeInfo.ConvertedNullability.Annotation)
#endif
            ;

        public static ITypeSymbol? GetTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.Type
#if !CODE_STYLE // TODO: Remove the #if once WithNullableAnnotation is available.
                ?.WithNullableAnnotation(typeInfo.Nullability.Annotation)
#endif
            ;
    }
}
