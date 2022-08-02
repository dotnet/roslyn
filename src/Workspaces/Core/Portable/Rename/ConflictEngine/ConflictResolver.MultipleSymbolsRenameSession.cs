// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal partial class ConflictResolver
    {
        private class MultipleSymbolRenameSession : AbstractRenameSession
        {
            public static Task<MultipleSymbolRenameSession> CreateAsync(
                ImmutableArray<(SymbolicRenameLocations, string, SymbolRenameOptions)> renameSymbolsInfo,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            private MultipleSymbolRenameSession(
                Solution solution,
                CancellationToken cancellationToken) : base(solution, ImmutableArray<SymbolKey>.Empty, default, cancellationToken)
            {
                // TODO: fall back option should be a global value among rename symbols
                throw new NotImplementedException();
            }

            protected override Task<(Solution partiallyRenamedSolution, ImmutableHashSet<DocumentId> unchangedDocuments)> AnnotateAndRename_WorkerAsync(Solution originalSolution, Solution partiallyRenamedSolution, HashSet<DocumentId> documentIdsThatGetsAnnotatedAndRenamed, RenamedSpansTracker renamedSpansTracker)
            {
                throw new NotImplementedException();
            }

            protected override ImmutableArray<ISymbol> GetSymbolRenamedInProjects(ProjectId projectId)
            {
                throw new NotImplementedException();
            }

            protected override Task<ImmutableHashSet<ISymbol>> GetNonConflictSymbolsAsync(Project projectProject)
            {
                throw new NotImplementedException();
            }

            protected override Task<ImmutableHashSet<RenamedSymbolInfo>> GetValidRenamedSymbolsInfoInCurrentSolutionAsync(MutableConflictResolution conflictResolution)
            {
                throw new NotImplementedException();
            }

            protected override Task<ImmutableArray<RenamedSymbolInfo>> GetDeclarationChangedSymbolsInfoAsync(MutableConflictResolution conflictResolution, ProjectId projectId)
            {
                throw new NotImplementedException();
            }

            protected override bool HasConflictForMetadataReference(RenameDeclarationLocationReference renameDeclarationLocationReference, ISymbol newReferencedSymbol)
            {
                throw new NotImplementedException();
            }

            public override Task<MutableConflictResolution> ResolveConflictsAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
