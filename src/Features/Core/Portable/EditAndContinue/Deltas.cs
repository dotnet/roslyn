// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Xml;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DataContract]
    internal sealed class Deltas
    {
        [DataMember(Order = 0)]
        public readonly Guid Mvid;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<byte> IL;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<byte> Metadata;

        [DataMember(Order = 3)]
        public readonly ImmutableArray<byte> Pdb;

        // Tokens of updated methods. The debugger enumerates this list 
        // updated methods containing active statements.
        [DataMember(Order = 4)]
        public readonly ImmutableArray<int> UpdatedMethods;

        [DataMember(Order = 5)]
        public readonly ImmutableArray<(string SourceFilePath, ImmutableArray<LineChange> Deltas)> LineEdits;

        [DataMember(Order = 6)]
        public readonly ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> NonRemappableRegions;

        [DataMember(Order = 7)]
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
            IL = il;
            Metadata = metadata;
            Pdb = pdb;
            UpdatedMethods = updatedMethods;
            NonRemappableRegions = nonRemappableRegions;
            ActiveStatementsInUpdatedMethods = activeStatementsInUpdatedMethods;
            LineEdits = lineEdits;
        }
    }
}
