// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal static class SymbolSearchOptionsStorage
    {
        internal static SymbolSearchOptions GetSymbolSearchOptions(this IGlobalOptionService globalOptions, string language)
            => new(
                SearchReferenceAssemblies: globalOptions.GetOption(SearchReferenceAssemblies, language),
                SearchNuGetPackages: globalOptions.GetOption(SearchNuGetPackages, language));

        private const string FeatureName = "SymbolSearchOptions";

        public static PerLanguageOption2<bool> SearchReferenceAssemblies =
            new(FeatureName, "SuggestForTypesInReferenceAssemblies", SymbolSearchOptions.Default.SearchReferenceAssemblies,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInReferenceAssemblies"));

        public static PerLanguageOption2<bool> SearchNuGetPackages =
            new(FeatureName, "SuggestForTypesInNuGetPackages", SymbolSearchOptions.Default.SearchNuGetPackages,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInNuGetPackages"));
    }
}
