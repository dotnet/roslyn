// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public readonly ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> NonRemappableRegions;
        public readonly ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> ActiveStatementsInUpdatedMethods;

        public Deltas(
            byte[] il,
            byte[] metadata,
            MemoryStream pdb,
            int[] updatedMethods,
            ImmutableArray<(DocumentId, ImmutableArray<LineChange>)> lineEdits,
            ImmutableArray<(ActiveMethodId, NonRemappableRegion)> nonRemappableRegions,
            ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> activeStatementsInUpdatedMethods,
            EmitDifferenceResult emitResult)
        {
            IL = new ILDelta(il);
            Metadata = new MetadataDelta(metadata);
            Pdb = new PdbDelta(pdb, updatedMethods);
            NonRemappableRegions = nonRemappableRegions;
            ActiveStatementsInUpdatedMethods = activeStatementsInUpdatedMethods;
            EmitResult = emitResult;
            LineEdits = lineEdits;
        }
    }
}
