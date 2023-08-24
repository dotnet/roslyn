﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal class EditAndContinueTestUtilities
{
    internal static string EncLogRowToString(EditAndContinueLogEntry row)
    {
        TableIndex tableIndex;
        MetadataTokens.TryGetTableIndex(row.Handle.Kind, out tableIndex);

        return string.Format(
            "Row({0}, TableIndex.{1}, EditAndContinueOperation.{2})",
            MetadataTokens.GetRowNumber(row.Handle),
            tableIndex,
            row.Operation);
    }

    internal static string EncMapRowToString(EntityHandle handle)
    {
        TableIndex tableIndex;
        MetadataTokens.TryGetTableIndex(handle.Kind, out tableIndex);

        return string.Format(
            "Handle({0}, TableIndex.{1})",
            MetadataTokens.GetRowNumber(handle),
            tableIndex);
    }

    internal static string AttributeRowToString(CustomAttributeRow row)
    {
        TableIndex parentTableIndex, constructorTableIndex;
        MetadataTokens.TryGetTableIndex(row.ParentToken.Kind, out parentTableIndex);
        MetadataTokens.TryGetTableIndex(row.ConstructorToken.Kind, out constructorTableIndex);

        return string.Format(
            "new CustomAttributeRow(Handle({0}, TableIndex.{1}), Handle({2}, TableIndex.{3}))",
            MetadataTokens.GetRowNumber(row.ParentToken),
            parentTableIndex,
            MetadataTokens.GetRowNumber(row.ConstructorToken),
            constructorTableIndex);
    }

    internal static bool IsDefinition(HandleKind kind)
        => kind is not (HandleKind.AssemblyReference or HandleKind.ModuleReference or HandleKind.TypeReference or HandleKind.MemberReference or HandleKind.TypeSpecification or HandleKind.MethodSpecification);
}
