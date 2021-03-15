﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassificationExtensions
    {
        public static string? GetClassification(this ITypeSymbol type)
            => type.TypeKind switch
            {
                TypeKind.Class => type.IsRecord ? ClassificationTypeNames.RecordClassName : ClassificationTypeNames.ClassName,
                TypeKind.Module => ClassificationTypeNames.ModuleName,
                TypeKind.Struct => ClassificationTypeNames.StructName,
                TypeKind.Interface => ClassificationTypeNames.InterfaceName,
                TypeKind.Enum => ClassificationTypeNames.EnumName,
                TypeKind.Delegate => ClassificationTypeNames.DelegateName,
                TypeKind.TypeParameter => ClassificationTypeNames.TypeParameterName,
                TypeKind.Dynamic => ClassificationTypeNames.Keyword,
                _ => null,
            };
    }
}
