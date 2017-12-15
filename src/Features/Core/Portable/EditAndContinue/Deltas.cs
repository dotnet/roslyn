// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class Deltas
    {
        public readonly ILDelta IL;
        public readonly MetadataDelta Metadata;

        public readonly ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> LineEdits;
        public readonly PdbDelta Pdb;
        public readonly EmitDifferenceResult EmitResult;
        public readonly ImmutableArray<(ActiveInstructionId, LinePositionSpan)> UpdatedActiveStatementSpans;

        public Deltas(
            byte[] il,
            byte[] metadata,
            int[] updatedMethods,
            ImmutableArray<(ActiveInstructionId, LinePositionSpan)> updatedActiveStatementSpans,
            MemoryStream pdb,
            ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> lineEdits,
            EmitDifferenceResult emitResult)
        {
            IL = new ILDelta(il);
            Metadata = new MetadataDelta(metadata);
            Pdb = new PdbDelta(pdb, updatedMethods);
            UpdatedActiveStatementSpans = updatedActiveStatementSpans;
            EmitResult = emitResult;
            LineEdits = lineEdits;
        }

    }
}
