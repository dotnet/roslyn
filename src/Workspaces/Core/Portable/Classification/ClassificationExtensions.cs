// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassificationExtensions
    {
        public static string? GetClassification(this ITypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                    return ClassificationTypeNames.ClassName;
                case TypeKind.Module:
                    return ClassificationTypeNames.ModuleName;
                case TypeKind.Struct:
                    return ClassificationTypeNames.StructName;
                case TypeKind.Interface:
                    return ClassificationTypeNames.InterfaceName;
                case TypeKind.Enum:
                    return ClassificationTypeNames.EnumName;
                case TypeKind.Delegate:
                    return ClassificationTypeNames.DelegateName;
                case TypeKind.TypeParameter:
                    return ClassificationTypeNames.TypeParameterName;
                case TypeKind.Dynamic:
                    return ClassificationTypeNames.Keyword;
                default:
                    return null;
            }
        }
    }
}
