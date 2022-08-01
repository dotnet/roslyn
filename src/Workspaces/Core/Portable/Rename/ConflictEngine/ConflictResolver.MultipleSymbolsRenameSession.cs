// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
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

            }

            private MultipleSymbolRenameSession(
                Solution solution,
                CancellationToken cancellationToken) : base(solution, cancellationToken)
            {
            }

            protected override Task<(Solution partiallyRenamedSolution, ImmutableHashSet<DocumentId> unchangedDocuments)> AnnotateAndRename_WorkerAsync(Solution originalSolution, Solution partiallyRenamedSolution, HashSet<DocumentId> documentIdsThatGetsAnnotatedAndRenamed, RenamedSpansTracker renamedSpansTracker)
            {
                throw new NotImplementedException();
            }

            protected override Task<bool> IdentifyConflictsAsync(HashSet<DocumentId> documentIdsForConflictResolution, IEnumerable<DocumentId> allDocumentIdsInProject, ProjectId projectId, MutableConflictResolution conflictResolution)
            {
                throw new NotImplementedException();
            }
        }
    }
}
