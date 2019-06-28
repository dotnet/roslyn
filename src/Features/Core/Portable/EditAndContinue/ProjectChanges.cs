// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// All active statements and the corresponding exception regions in changed documents.
        /// </summary>
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> NewActiveStatements;

        public ProjectChanges(
            ImmutableArray<SemanticEdit> semanticEdits,
            ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> lineChanges,
            ImmutableArray<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)> newActiveStatements)
        {
            Debug.Assert(!semanticEdits.IsDefault);
            Debug.Assert(!lineChanges.IsDefault);
            Debug.Assert(!newActiveStatements.IsDefault);

            SemanticEdits = semanticEdits;
            LineChanges = lineChanges;
            NewActiveStatements = newActiveStatements;
        }
    }
}
