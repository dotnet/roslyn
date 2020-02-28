﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
