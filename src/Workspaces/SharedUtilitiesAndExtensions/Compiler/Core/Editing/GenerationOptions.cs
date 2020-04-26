// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editing
{
    internal class GenerationOptions
    {
        public static readonly PerLanguageOption2<bool> PlaceSystemNamespaceFirst = new PerLanguageOption2<bool>(nameof(GenerationOptions),
            CodeStyleOptionGroups.Usings,
            nameof(PlaceSystemNamespaceFirst), defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("dotnet_sort_system_directives_first"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PlaceSystemNamespaceFirst")});

        public static readonly PerLanguageOption2<bool> SeparateImportDirectiveGroups = new PerLanguageOption2<bool>(
            nameof(GenerationOptions), CodeStyleOptionGroups.Usings, nameof(SeparateImportDirectiveGroups), defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("dotnet_separate_import_directive_groups"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(SeparateImportDirectiveGroups)}")});

        public static readonly ImmutableArray<IOption2> AllOptions = ImmutableArray.Create<IOption2>(
            PlaceSystemNamespaceFirst,
            SeparateImportDirectiveGroups);
    }
}
