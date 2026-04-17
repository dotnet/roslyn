// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

[DataContract]
#if NETFRAMEWORK
[System.Serializable] // We need to this to be able to serialize across the AppDomain boundary
#endif
internal sealed record FileGlobs(
    [property: DataMember(Order = 0)] ImmutableArray<string> Includes,
    [property: DataMember(Order = 1)] ImmutableArray<string> Excludes,
    [property: DataMember(Order = 2)] ImmutableArray<string> Removes
);
