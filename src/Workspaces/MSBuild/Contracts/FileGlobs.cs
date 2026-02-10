// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

[DataContract]
internal sealed record FileGlobs(
    [property: DataMember(Order = 0)] string[] Includes,
    [property: DataMember(Order = 1)] string[] Excludes,
    [property: DataMember(Order = 2)] string[] Removes
);
