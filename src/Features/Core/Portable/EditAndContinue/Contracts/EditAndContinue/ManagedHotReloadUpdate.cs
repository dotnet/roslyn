// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue.Contracts;

[DataContract]
internal readonly struct ManagedHotReloadUpdate
{
    [DataMember(Order = 0)] public Guid Module { get; }
    [DataMember(Order = 1)] public string ModuleName { get; }
    [DataMember(Order = 2)] public ImmutableArray<byte> ILDelta { get; }
    [DataMember(Order = 3)] public ImmutableArray<byte> MetadataDelta { get; }
    [DataMember(Order = 4)] public ImmutableArray<byte> PdbDelta { get; }
    [DataMember(Order = 5)] public ImmutableArray<SequencePointUpdates> SequencePoints { get; }
    [DataMember(Order = 6)] public ImmutableArray<int> UpdatedMethods { get; }
    [DataMember(Order = 7)] public ImmutableArray<int> UpdatedTypes { get; }
    [DataMember(Order = 8)] public ImmutableArray<ManagedActiveStatementUpdate> ActiveStatements { get; }
    [DataMember(Order = 9)] public ImmutableArray<ManagedExceptionRegionUpdate> ExceptionRegions { get; }
    [DataMember(Order = 10)] public ImmutableArray<string> RequiredCapabilities { get; }
    
    public ManagedHotReloadUpdate(
        Guid module,
        string moduleName,
        ImmutableArray<byte> ilDelta,
        ImmutableArray<byte> metadataDelta,
        ImmutableArray<byte> pdbDelta,
        ImmutableArray<int> updatedTypes,
        ImmutableArray<string> requiredCapabilities,
        ImmutableArray<int> updatedMethods,
        ImmutableArray<SequencePointUpdates> sequencePoints,
        ImmutableArray<ManagedActiveStatementUpdate> activeStatements,
        ImmutableArray<ManagedExceptionRegionUpdate> exceptionRegions)
    {
        Module = module;
        ModuleName = moduleName;
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
}
