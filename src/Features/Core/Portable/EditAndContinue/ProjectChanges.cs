// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ProjectChanges
    {
        /// <summary>
        /// All semantic changes made in changed documents.
        /// </summary>
        public readonly ImmutableArray<SemanticEdit> SemanticEdits;

        /// <summary>
        /// All line changes made in changed documents.
        /// </summary>
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<LineChange> Changes)> LineChanges;

        /// <summary>
        /// All symbols added in changed documents.
        /// </summary>
        public readonly ImmutableHashSet<ISymbol> AddedSymbols;

        /// <summary>
        /// All active statements and the corresponding exception regions in changed documents.
        /// </summary>
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> NewActiveStatements;

        public ProjectChanges(
            ImmutableArray<SemanticEdit> semanticEdits,
            ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> lineChanges,
            ImmutableHashSet<ISymbol> addedSymbols,
            ImmutableArray<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)> newActiveStatements)
        {
            Debug.Assert(!semanticEdits.IsDefault);
            Debug.Assert(!lineChanges.IsDefault);
            Debug.Assert(addedSymbols != null);
            Debug.Assert(!newActiveStatements.IsDefault);

            SemanticEdits = semanticEdits;
            LineChanges = lineChanges;
            AddedSymbols = addedSymbols;
            NewActiveStatements = newActiveStatements;
        }
    }
}
