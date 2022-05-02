// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    [ExportSolutionOptionProvider, Shared]
    internal sealed class SymbolSearchOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SymbolSearchOptions()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SuggestForTypesInReferenceAssemblies,
            SuggestForTypesInNuGetPackages);

        private const string FeatureName = "SymbolSearchOptions";

        public static PerLanguageOption2<bool> SuggestForTypesInReferenceAssemblies =
            new(FeatureName, "SuggestForTypesInReferenceAssemblies", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInReferenceAssemblies"));

        public static PerLanguageOption2<bool> SuggestForTypesInNuGetPackages =
            new(FeatureName, "SuggestForTypesInNuGetPackages", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInNuGetPackages"));
    }
}
