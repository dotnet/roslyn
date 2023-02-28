﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal static class SymbolSearchOptionsStorage
    {
        internal static SymbolSearchOptions GetSymbolSearchOptions(this IGlobalOptionService globalOptions, string language)
            => new()
            {
                SearchReferenceAssemblies = globalOptions.GetOption(SearchReferenceAssemblies, language),
                SearchNuGetPackages = globalOptions.GetOption(SearchNuGetPackages, language)
            };

        public static PerLanguageOption2<bool> SearchReferenceAssemblies =
            new("dotnet_search_reference_assemblies", SymbolSearchOptions.Default.SearchReferenceAssemblies);

        public static PerLanguageOption2<bool> SearchNuGetPackages =
            new("dotnet_search_nuget_packages", SymbolSearchOptions.Default.SearchNuGetPackages);
    }
}
