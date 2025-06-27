// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

[DataContract]
internal readonly struct ManagedHotReloadUpdate(
    Guid module,
    string moduleName,
    ProjectId projectId,
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
    [DataMember(Name = "module")]
    public Guid Module { get; } = module;

    [DataMember(Name = "moduleName")]
    public string ModuleName { get; } = moduleName;

    [DataMember(Name = "projectId")]
    public ProjectId ProjectId { get; } = projectId;

    [DataMember(Name = "ilDelta")]
    public ImmutableArray<byte> ILDelta { get; } = ilDelta;

    [DataMember(Name = "metadataDelta")]
    public ImmutableArray<byte> MetadataDelta { get; } = metadataDelta;

    [DataMember(Name = "pdbDelta")]
    public ImmutableArray<byte> PdbDelta { get; } = pdbDelta;

    [DataMember(Name = "updatedTypes")]
    public ImmutableArray<int> UpdatedTypes { get; } = updatedTypes;

    [DataMember(Name = "requiredCapabilities")]
    public ImmutableArray<string> RequiredCapabilities { get; } = requiredCapabilities;

    [DataMember(Name = "updatedMethods")]
    public ImmutableArray<int> UpdatedMethods { get; } = updatedMethods;

    [DataMember(Name = "sequencePoints")]
    public ImmutableArray<SequencePointUpdates> SequencePoints { get; } = sequencePoints;

    [DataMember(Name = "activeStatements")]
    public ImmutableArray<ManagedActiveStatementUpdate> ActiveStatements { get; } = activeStatements;

    [DataMember(Name = "exceptionRegions")]
    public ImmutableArray<ManagedExceptionRegionUpdate> ExceptionRegions { get; } = exceptionRegions;
}
