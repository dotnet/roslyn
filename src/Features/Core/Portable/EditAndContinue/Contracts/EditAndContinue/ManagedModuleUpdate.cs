// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue.Contracts
{
    /// <summary>
    /// Information about a collection of managed updates under a target module.
    /// </summary>
    [DataContract]
    internal readonly struct ManagedModuleUpdates
    {
        /// <summary>
        /// Creates a new ManagedModuleUpdates.
        /// </summary>
        /// <param name="status">Update status.</param>
        /// <param name="updates">Collection of the module updates.</param>
        public ManagedModuleUpdates(
            ManagedModuleUpdateStatus status,
            ImmutableArray<ManagedModuleUpdate> updates)
        {
            Status = status;
            Updates = updates;
        }

        /// <summary>
        /// This is the kind of change made to the modules owned by the provider.
        /// The change is aggregated across all modules owned by the provider: if one module has a rude edit
        /// and another one has valid change, the resulting kind is <see cref="ManagedModuleUpdateStatus.Blocked"/>.
        /// If <see cref="Updates"/> is empty, this has a value of <see cref="ManagedModuleUpdateStatus.None"/>.
        /// </summary>
        [DataMember(Name = "status")]
        public ManagedModuleUpdateStatus Status { get; }

        /// <summary>
        /// Expected to be empty if Status != Ready.
        /// </summary>
        [DataMember(Name = "updates")]
        public ImmutableArray<ManagedModuleUpdate> Updates { get; }
    }

    /// <summary>
    /// Information about a single update. This corresponds to an edit made by the user.
    /// </summary>
    [DataContract]
    internal readonly struct ManagedModuleUpdate
    {
        /// <summary>
        /// Creates a new ManagedModuleUpdate.
        /// </summary>
        /// <param name="module">Module ID which the update belongs to.</param>
        /// <param name="ilDelta">IL delta from the change.</param>
        /// <param name="metadataDelta">Metadata delta from the change.</param>
        /// <param name="pdbDelta">PDB delta from the change.</param>
        /// <param name="sequencePoints">Sequence points affected by the symbolic data change.</param>
        /// <param name="updatedMethods">Methods affected by the update.</param>
        /// <param name="activeStatements">Active statements affected by the update.</param>
        /// <param name="exceptionRegions">Exception regions affected by the update.</param>
        /// <param name="updatedTypes">List of updated TypeDefs.</param>
        public ManagedModuleUpdate(
            Guid module,
            ImmutableArray<byte> ilDelta,
            ImmutableArray<byte> metadataDelta,
            ImmutableArray<byte> pdbDelta,
            ImmutableArray<SequencePointUpdates> sequencePoints,
            ImmutableArray<int> updatedMethods,
            ImmutableArray<int> updatedTypes,
            ImmutableArray<ManagedActiveStatementUpdate> activeStatements,
            ImmutableArray<ManagedExceptionRegionUpdate> exceptionRegions,
            EditAndContinueCapabilities requiredCapabilities)
        {
            Module = module;

            ILDelta = ilDelta;
            MetadataDelta = metadataDelta;

            PdbDelta = pdbDelta;
            SequencePoints = sequencePoints;

            UpdatedMethods = updatedMethods;
            UpdatedTypes = updatedTypes;

            ActiveStatements = activeStatements;
            ExceptionRegions = exceptionRegions;
            RequiredCapabilities = requiredCapabilities;
        }

        /// <summary>
        /// Module version Identifier which the managed update was applied. This uniquely 
        /// identifies the symbol file.For Microsoft C++ or Microsoft .NET Framework binaries,
        /// this is a unique value which is embedded in an exe/dll by linkers/compilers when the
        /// dll/exe is built. A new value is generated each time that the dll/exe is compiled.
        /// </summary>
        [DataMember(Name = "module")]
        public Guid Module { get; }

        /// <summary>
        /// Collection of IL deltas affected by the update. Required by ICorDebugModule2::ApplyChanges.
        /// </summary>
        [DataMember(Name = "ilDelta")]
        public ImmutableArray<byte> ILDelta { get; }

        /// <summary>
        /// Collection of metadata deltas affected by the update. Required by ICorDebugModule2::ApplyChanges.
        /// </summary>
        [DataMember(Name = "metadataDelta")]
        public ImmutableArray<byte> MetadataDelta { get; }

        /// <summary>
        /// Collection of PDB deltas regarding the symbol information affected by the update.
        /// </summary>
        [DataMember(Name = "pdbDelta")]
        public ImmutableArray<byte> PdbDelta { get; }

        /// <summary>
        /// Collection of sequence points affected by the update.
        /// This will alter the line number for one or more existing sequence point in the symbolic data.
        /// </summary>
        [DataMember(Name = "sequencePoints")]
        public ImmutableArray<SequencePointUpdates> SequencePoints { get; }

        /// <summary>
        /// Method token for all the methods affected by the update.
        /// </summary>
        [DataMember(Name = "updatedMethods")]
        public ImmutableArray<int> UpdatedMethods { get; }

        /// <summary>
        /// List of TypeDefs that have been modified during an edit.
        /// This is passed on to the CLR as an event on each EnC update.
        /// </summary>
        [DataMember(Name = "updatedTypes")]
        public ImmutableArray<int> UpdatedTypes { get; }

        /// <summary>
        /// Collection of active statements affected by the update.
        /// This will not list duplicate active statements in multiple threads and will report an unique one instead.
        /// </summary>
        [DataMember(Name = "activeStatements")]
        public ImmutableArray<ManagedActiveStatementUpdate> ActiveStatements { get; }

        /// <summary>
        /// Collection of exception regions affected by the update. An exception region is enclosed by a try/catch/finally block.
        /// </summary>
        [DataMember(Name = "exceptionRegions")]
        public ImmutableArray<ManagedExceptionRegionUpdate> ExceptionRegions { get; }

        [DataMember(Name = "requiredCapabilities")]
        public EditAndContinueCapabilities RequiredCapabilities { get; }
    }
}
