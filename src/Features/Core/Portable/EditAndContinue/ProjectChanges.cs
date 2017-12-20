// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
        public readonly ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> LineChanges;

        /// <summary>
        /// All active statements in changed documents.
        /// </summary>
        public readonly ImmutableArray<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)> ActiveStatements;

        public ProjectChanges(
            ImmutableArray<SemanticEdit> semanticEdits,
            ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> lineChanges,
            ImmutableArray<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)> activeStatements)
        {
            SemanticEdits = semanticEdits;
            LineChanges = lineChanges;
            ActiveStatements = activeStatements;
        }
    }
}
