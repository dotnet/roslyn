// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal struct ProjectChanges
    {
        public readonly List<SemanticEdit> SemanticEdits;
        public readonly List<KeyValuePair<DocumentId, ImmutableArray<LineChange>>> LineChanges;

        public ProjectChanges(
            List<SemanticEdit> semanticEdits,
            List<KeyValuePair<DocumentId, ImmutableArray<LineChange>>> lineChanges)
        {
            this.SemanticEdits = semanticEdits;
            this.LineChanges = lineChanges;
        }
    }
}
