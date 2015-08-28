// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class Deltas
    {
        public readonly ILDelta IL;
        public readonly MetadataDelta Metadata;

        public readonly List<KeyValuePair<DocumentId, ImmutableArray<LineChange>>> LineEdits;
        public readonly PdbDelta Pdb;
        public readonly EmitDifferenceResult EmitResult;

        public Deltas(
            byte[] il,
            byte[] metadata,
            int[] updatedMethods,
            MemoryStream pdb,
            List<KeyValuePair<DocumentId, ImmutableArray<LineChange>>> lineEdits,
            EmitDifferenceResult emitResult)
        {
            this.IL = new ILDelta(il);
            this.Metadata = new MetadataDelta(metadata);
            this.Pdb = new PdbDelta(pdb, updatedMethods);
            this.EmitResult = emitResult;
            this.LineEdits = lineEdits;
        }
    }
}
