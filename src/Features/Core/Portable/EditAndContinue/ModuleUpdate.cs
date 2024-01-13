// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
internal readonly record struct ModuleUpdates(
    [property: DataMember(Order = 0)] ModuleUpdateStatus Status,
    [property: DataMember(Order = 1)] ImmutableArray<ManagedHotReloadUpdate> Updates);
