// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassificationExtensions
    {
        public static ClassificationTypeKind? GetClassification(this ITypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                    return ClassificationTypeKind.ClassName;
                case TypeKind.Module:
                    return ClassificationTypeKind.ModuleName;
                case TypeKind.Struct:
                    return ClassificationTypeKind.StructName;
                case TypeKind.Interface:
                    return ClassificationTypeKind.InterfaceName;
                case TypeKind.Enum:
                    return ClassificationTypeKind.EnumName;
                case TypeKind.Delegate:
                    return ClassificationTypeKind.DelegateName;
                case TypeKind.TypeParameter:
                    return ClassificationTypeKind.TypeParameterName;
                case TypeKind.Dynamic:
                    return ClassificationTypeKind.Keyword;
                default:
                    return null;
            }
        }
    }
}