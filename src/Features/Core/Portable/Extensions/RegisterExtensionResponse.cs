// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Extensions;

[DataContract]
internal readonly record struct GetExtensionMessageNamesResponse(
    [property: DataMember(Order = 0)] ImmutableArray<string> WorkspaceMessageHandlers,
    [property: DataMember(Order = 1)] ImmutableArray<string> DocumentMessageHandlers);
