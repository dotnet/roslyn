// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.OrganizeImports;

[DataContract]
internal readonly record struct OrganizeImportsOptions
{
    [DataMember] public bool PlaceSystemNamespaceFirst { get; init; } = AddImportPlacementOptions.Default.PlaceSystemNamespaceFirst;
    [DataMember] public bool SeparateImportDirectiveGroups { get; init; } = SyntaxFormattingOptions.CommonDefaults.SeparateImportDirectiveGroups;
    [DataMember] public string NewLine { get; init; } = LineFormattingOptions.Default.NewLine;

    public OrganizeImportsOptions()
    {
    }

    public static readonly OrganizeImportsOptions Default = new();
}
