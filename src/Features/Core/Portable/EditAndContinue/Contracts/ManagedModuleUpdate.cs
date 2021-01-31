// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal sealed class ManagedModuleUpdate
    {
        [DataMember(Order = 0)]
        public readonly Guid Module;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<byte> ILDelta;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<byte> MetadataDelta;

        [DataMember(Order = 3)]
        public readonly ImmutableArray<byte> PdbDelta;

        [DataMember(Order = 4)]
        public readonly ImmutableArray<SequencePointUpdates> SequencePoints;

        // Tokens of updated methods. The debugger enumerates this list 
        // updated methods containing active statements.
        [DataMember(Order = 5)]
        public readonly ImmutableArray<int> UpdatedMethods;

        [DataMember(Order = 6)]
        public readonly ImmutableArray<ManagedActiveStatementUpdate> ActiveStatements;

        [DataMember(Order = 7)]
        public readonly ImmutableArray<ManagedExceptionRegionUpdate> ExceptionRegions;

        public ManagedModuleUpdate(
            Guid module,
            ImmutableArray<byte> ilDelta,
            ImmutableArray<byte> metadataDelta,
            ImmutableArray<byte> pdbDelta,
            ImmutableArray<SequencePointUpdates> sequencePoints,
            ImmutableArray<int> updatedMethods,
            ImmutableArray<ManagedActiveStatementUpdate> activeStatements,
            ImmutableArray<ManagedExceptionRegionUpdate> exceptionRegions)
        {
            Module = module;
            ILDelta = ilDelta;
            MetadataDelta = metadataDelta;
            PdbDelta = pdbDelta;
            UpdatedMethods = updatedMethods;
            ActiveStatements = activeStatements;
            SequencePoints = sequencePoints;
            ExceptionRegions = exceptionRegions;
        }
    }
}

