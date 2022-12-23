// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editing
{
    internal class GenerationOptions
    {
        public static readonly PerLanguageOption2<bool> PlaceSystemNamespaceFirst = new(
            CodeStyleOptionGroups.Usings, "dotnet_sort_system_directives_first",
            AddImportPlacementOptions.Default.PlaceSystemNamespaceFirst,
            EditorConfigStorageLocation.ForBoolOption("dotnet_sort_system_directives_first"));

        public static readonly PerLanguageOption2<bool> SeparateImportDirectiveGroups = new(
            CodeStyleOptionGroups.Usings, "dotnet_separate_import_directive_groups",
            SyntaxFormattingOptions.CommonOptions.Default.SeparateImportDirectiveGroups,
            EditorConfigStorageLocation.ForBoolOption("dotnet_separate_import_directive_groups"));

        public static readonly ImmutableArray<IOption2> AllOptions = ImmutableArray.Create<IOption2>(
            PlaceSystemNamespaceFirst,
            SeparateImportDirectiveGroups);
    }
}
