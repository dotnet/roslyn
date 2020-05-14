// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class Deltas
    {
        public readonly Guid Mvid;
        public readonly ILDelta IL;
        public readonly MetadataDelta Metadata;

        public readonly ImmutableArray<(string SourceFilePath, ImmutableArray<LineChange> Deltas)> LineEdits;
        public readonly PdbDelta Pdb;
        public readonly ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> NonRemappableRegions;
        public readonly ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> ActiveStatementsInUpdatedMethods;

        public Deltas(
            Guid mvid,
            ImmutableArray<byte> il,
            ImmutableArray<byte> metadata,
            ImmutableArray<byte> pdb,
            ImmutableArray<int> updatedMethods,
            ImmutableArray<(string, ImmutableArray<LineChange>)> lineEdits,
            ImmutableArray<(ActiveMethodId, NonRemappableRegion)> nonRemappableRegions,
            ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> activeStatementsInUpdatedMethods)
        {
            Mvid = mvid;
            IL = new ILDelta(il);
            Metadata = new MetadataDelta(metadata);
            Pdb = new PdbDelta(pdb, updatedMethods);
            NonRemappableRegions = nonRemappableRegions;
            ActiveStatementsInUpdatedMethods = activeStatementsInUpdatedMethods;
            LineEdits = lineEdits;
        }
    }
}
