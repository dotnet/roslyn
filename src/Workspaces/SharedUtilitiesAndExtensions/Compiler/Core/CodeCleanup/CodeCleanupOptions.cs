﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.CodeCleanup;

[DataContract]
internal sealed record class CodeCleanupOptions
{
    [DataMember] public required SyntaxFormattingOptions FormattingOptions { get; init; }
    [DataMember] public required SimplifierOptions SimplifierOptions { get; init; }
    [DataMember] public AddImportPlacementOptions AddImportOptions { get; init; } = AddImportPlacementOptions.Default;
    [DataMember] public DocumentFormattingOptions DocumentFormattingOptions { get; init; } = DocumentFormattingOptions.Default;

    public OrganizeImportsOptions GetOrganizeImportsOptions()
        => new()
        {
            SeparateImportDirectiveGroups = FormattingOptions.SeparateImportDirectiveGroups,
            PlaceSystemNamespaceFirst = AddImportOptions.PlaceSystemNamespaceFirst,
            NewLine = FormattingOptions.LineFormatting.NewLine,
        };
}
