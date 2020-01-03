// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        public static ITypeSymbol? GetConvertedTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullableAnnotation(typeInfo.ConvertedNullability.Annotation);

        public static ITypeSymbol? GetTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullableAnnotation(typeInfo.Nullability.Annotation);
    }
}
