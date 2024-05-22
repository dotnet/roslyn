// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.FindUsages;

/// <summary>
/// Describes an assembly (source or metadata) that contains a found symbol definition or usage.
/// </summary>
[DataContract]
internal readonly record struct AssemblyLocation(
    [property: DataMember(Order = 0)] string Name,
    [property: DataMember(Order = 1)] Version Version,
    [property: DataMember(Order = 2)] string? FilePath);
