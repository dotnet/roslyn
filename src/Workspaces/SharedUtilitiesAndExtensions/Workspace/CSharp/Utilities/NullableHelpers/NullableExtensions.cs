// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal static partial class NullableExtensions
{
    extension(TypeInfo typeInfo)
    {
        public ITypeSymbol? GetConvertedTypeWithAnnotatedNullability()
        => typeInfo.ConvertedType?.WithNullableAnnotation(typeInfo.ConvertedNullability.Annotation);

        public ITypeSymbol? GetTypeWithAnnotatedNullability()
            => typeInfo.Type?.WithNullableAnnotation(typeInfo.Nullability.Annotation);
    }
}
