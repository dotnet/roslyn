﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal static class SymbolSearchOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\SymbolSearch\";

        public static readonly Option<bool> Enabled = new Option<bool>(
            nameof(SymbolSearchOptions), nameof(Enabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(Enabled)));

        public static PerLanguageOption<bool> SuggestForTypesInReferenceAssemblies =
            new PerLanguageOption<bool>(nameof(SymbolSearchOptions), nameof(SuggestForTypesInReferenceAssemblies), defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInReferenceAssemblies"));

        public static PerLanguageOption<bool> SuggestForTypesInNuGetPackages =
            new PerLanguageOption<bool>(nameof(SymbolSearchOptions), nameof(SuggestForTypesInNuGetPackages), defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInNuGetPackages"));
    }
}
