// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal static class TypeInfoExtensions
    {
        public static ITypeSymbol GetConvertedTypeWithFlowNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullability(typeInfo.Nullability.FlowState);

        public static ITypeSymbol GetConvertedTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.ConvertedType?.WithNullability(typeInfo.Nullability.Annotation);

        public static ITypeSymbol GetTypeWithFlowNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullability(typeInfo.Nullability.FlowState);

        public static ITypeSymbol GetTypeWithAnnotatedNullability(this TypeInfo typeInfo)
            => typeInfo.Type?.WithNullability(typeInfo.Nullability.Annotation);
    }
}
