// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal static class SemanticSearchDefinitionItemFactory
{
    private static readonly FindReferencesSearchOptions s_findReferencesSearchOptions = new()
    {
        DisplayAllDefinitions = true,
    };

    public static ValueTask<DefinitionItem> CreateAsync(Solution solution, ISymbol symbol, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
        => symbol.ToClassifiedDefinitionItemAsync(
            classificationOptions, solution, s_findReferencesSearchOptions, isPrimary: true, includeHiddenLocations: false, cancellationToken);
}
