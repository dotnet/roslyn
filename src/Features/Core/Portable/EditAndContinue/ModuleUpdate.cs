// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
internal readonly record struct ModuleUpdates(
    [property: DataMember(Order = 0)] ModuleUpdateStatus Status,
    [property: DataMember(Order = 1)] ImmutableArray<ManagedHotReloadUpdate> Updates);
