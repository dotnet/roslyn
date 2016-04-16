// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISymbolDisplayService : ILanguageService
    {
        string ToDisplayString(ISymbol symbol, SymbolDisplayFormat format = null);
        string ToMinimalDisplayString(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDisplayFormat format = null);
        ImmutableArray<SymbolDisplayPart> ToDisplayParts(ISymbol symbol, SymbolDisplayFormat format = null);
        ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDisplayFormat format = null);
        Task<string> ToDescriptionStringAsync(Workspace workspace, SemanticModel semanticModel, int position, ISymbol symbol, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> ToDescriptionStringAsync(Workspace workspace, SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default(CancellationToken));
        Task<ImmutableArray<SymbolDisplayPart>> ToDescriptionPartsAsync(Workspace workspace, SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default(CancellationToken));
        Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<SymbolDisplayPart>>> ToDescriptionGroupsAsync(Workspace workspace, SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, CancellationToken cancellationToken = default(CancellationToken));
    }
}
