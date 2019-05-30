// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    [ExportOptionProvider, Shared]
    internal class SymbolSearchOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public SymbolSearchOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SymbolSearchOptions.Enabled,
            SymbolSearchOptions.SuggestForTypesInReferenceAssemblies,
            SymbolSearchOptions.SuggestForTypesInNuGetPackages);
    }
}
