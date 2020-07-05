// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Xml;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class Deltas
    {
        public sealed class Data
        {
            public Guid Mvid;
            public ImmutableArray<byte> IL;
            public ImmutableArray<byte> Metadata;
            public ImmutableArray<byte> Pdb;
            public ImmutableArray<(string SourceFilePath, ImmutableArray<(int OldLine, int NewLine)> Deltas)> LineEdits;
            public ImmutableArray<int> UpdatedMethods;
            public ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> NonRemappableRegions;
            public ImmutableArray<(Guid ThreadId, Guid OldModuleId, int OldMethodToken, int OldMethodVersion, int OldILOffset, LinePositionSpan NewSpan)> ActiveStatementsInUpdatedMethods;

            public Deltas Deserialize()
                => new Deltas(
                    Mvid,
                    IL,
                    Metadata,
                    Pdb,
                    UpdatedMethods,
                    LineEdits.SelectAsArray(e => (e.SourceFilePath, e.Deltas.SelectAsArray(ld => new LineChange(ld.OldLine, ld.NewLine)))),
                    NonRemappableRegions,
                    ActiveStatementsInUpdatedMethods.SelectAsArray(s => (s.ThreadId, new ActiveInstructionId(s.OldModuleId, s.OldMethodToken, s.OldMethodVersion, s.OldILOffset), s.NewSpan)));
        }

        public readonly Guid Mvid;
        public readonly ImmutableArray<byte> IL;
        public readonly ImmutableArray<byte> Metadata;
        public readonly ImmutableArray<byte> Pdb;

        public readonly ImmutableArray<(string SourceFilePath, ImmutableArray<LineChange> Deltas)> LineEdits;

        // Tokens of updated methods. The debugger enumerates this list 
        // updated methods containing active statements.
        public readonly ImmutableArray<int> UpdatedMethods;

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
            IL = il;
            Metadata = metadata;
            Pdb = pdb;
            UpdatedMethods = updatedMethods;
            NonRemappableRegions = nonRemappableRegions;
            ActiveStatementsInUpdatedMethods = activeStatementsInUpdatedMethods;
            LineEdits = lineEdits;
        }

        public Data Serialize()
            => new Data()
            {
                Mvid = Mvid,
                IL = IL,
                Metadata = Metadata,
                Pdb = Pdb,
                UpdatedMethods = UpdatedMethods,
                LineEdits = LineEdits.
                    SelectAsArray(e => (e.SourceFilePath, e.Deltas.SelectAsArray(ld => (ld.OldLine, ld.NewLine)))),
                NonRemappableRegions = NonRemappableRegions,
                ActiveStatementsInUpdatedMethods = ActiveStatementsInUpdatedMethods.
                    SelectAsArray(s => (s.ThreadId, s.OldInstructionId.MethodId.ModuleId, s.OldInstructionId.MethodId.Token, s.OldInstructionId.MethodId.Version, s.OldInstructionId.ILOffset, s.NewSpan))
            };
    }
}
