// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageService
{
    internal interface ISymbolDisplayService : ILanguageService
    {
        Task<string> ToDescriptionStringAsync(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDescriptionOptions options, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default);
        Task<string> ToDescriptionStringAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default);
        Task<ImmutableArray<SymbolDisplayPart>> ToDescriptionPartsAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default);
        Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>> ToDescriptionGroupsAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, CancellationToken cancellationToken = default);
    }
}
