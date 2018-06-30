﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class DestructorSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind == MethodKind.Destructor;
        }

        protected override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            SymbolFinderOptions options,
            CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId>();
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            SymbolFinderOptions options,
            CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Document>();
        }

        protected override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            SemanticModel semanticModel,
            SymbolFinderOptions options,
            CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<ReferenceLocation>();
        }
    }
}
