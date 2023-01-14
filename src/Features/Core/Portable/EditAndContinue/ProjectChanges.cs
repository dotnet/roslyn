// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;

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
        public readonly ImmutableArray<SequencePointUpdates> LineChanges;

        /// <summary>
        /// All symbols added in changed documents.
        /// </summary>
        public readonly ImmutableHashSet<ISymbol> AddedSymbols;

        /// <summary>
        /// All active statements and the corresponding exception regions in changed documents.
        /// </summary>
        public readonly ImmutableArray<DocumentActiveStatementChanges> ActiveStatementChanges;

        /// <summary>
        /// Runtime capabilities required to apply the changes.
        /// </summary>
        public readonly EditAndContinueCapabilities RequiredCapabilities;

        public ProjectChanges(
            ImmutableArray<SemanticEdit> semanticEdits,
            ImmutableArray<SequencePointUpdates> lineChanges,
            ImmutableHashSet<ISymbol> addedSymbols,
            ImmutableArray<DocumentActiveStatementChanges> activeStatementChanges,
            EditAndContinueCapabilities requiredCapabilities)
        {
            Debug.Assert(!semanticEdits.IsDefault);
            Debug.Assert(!lineChanges.IsDefault);
            Debug.Assert(!activeStatementChanges.IsDefault);
            Debug.Assert(requiredCapabilities != EditAndContinueCapabilities.None);

            SemanticEdits = semanticEdits;
            LineChanges = lineChanges;
            AddedSymbols = addedSymbols;
            ActiveStatementChanges = activeStatementChanges;
            RequiredCapabilities = requiredCapabilities;
        }
    }
}
