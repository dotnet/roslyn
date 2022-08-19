// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
internal readonly record struct ModuleUpdate(
    [property: DataMember(Order = 0)] Guid Module,
    [property: DataMember(Order = 1)] ImmutableArray<byte> ILDelta,
    [property: DataMember(Order = 2)] ImmutableArray<byte> MetadataDelta,
    [property: DataMember(Order = 3)] ImmutableArray<byte> PdbDelta,
    [property: DataMember(Order = 4)] ImmutableArray<SequencePointUpdates> SequencePoints,
    [property: DataMember(Order = 5)] ImmutableArray<int> UpdatedMethods,
    [property: DataMember(Order = 6)] ImmutableArray<int> UpdatedTypes,
    [property: DataMember(Order = 7)] ImmutableArray<ManagedActiveStatementUpdate> ActiveStatements,
    [property: DataMember(Order = 8)] ImmutableArray<ManagedExceptionRegionUpdate> ExceptionRegions,
    [property: DataMember(Order = 9)] EditAndContinueCapabilities RequiredCapabilities);

[DataContract]
internal readonly record struct ModuleUpdates(
    [property: DataMember(Order = 0)] ModuleUpdateStatus Status,
    [property: DataMember(Order = 1)] ImmutableArray<ModuleUpdate> Updates);
