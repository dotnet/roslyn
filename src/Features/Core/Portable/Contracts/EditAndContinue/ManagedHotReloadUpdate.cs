// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

[DataContract]
internal readonly struct ManagedHotReloadUpdate
{
    [DataMember(Name = "module")]
    public Guid Module { get; }

    [DataMember(Name = "moduleName")]
    public string ModuleName { get; }

    [DataMember(Name = "ilDelta")]
    public ImmutableArray<byte> ILDelta { get; }

    [DataMember(Name = "metadataDelta")]
    public ImmutableArray<byte> MetadataDelta { get; }

    [DataMember(Name = "pdbDelta")]
    public ImmutableArray<byte> PdbDelta { get; }

    [DataMember(Name = "updatedTypes")]
    public ImmutableArray<int> UpdatedTypes { get; }

    [DataMember(Name = "requiredCapabilities")]
    public ImmutableArray<string> RequiredCapabilities { get; }

    [DataMember(Name = "updatedMethods")]
    public ImmutableArray<int> UpdatedMethods { get; }

    [DataMember(Name = "sequencePoints")]
    public ImmutableArray<SequencePointUpdates> SequencePoints { get; }

    [DataMember(Name = "activeStatements")]
    public ImmutableArray<ManagedActiveStatementUpdate> ActiveStatements { get; }

    [DataMember(Name = "exceptionRegions")]
    public ImmutableArray<ManagedExceptionRegionUpdate> ExceptionRegions { get; }

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
