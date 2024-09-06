// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Represents a part of solution snapshot along with its checksum.
/// </summary>
internal readonly struct SolutionAsset
{
    /// <summary>
    /// Indicates what kind of object it.
    /// 
    /// Used in transportation framework and deserialization service to hand shake how to send over data and
    /// deserialize serialized data.
    /// </summary>
    public readonly WellKnownSynchronizationKind Kind;

    /// <summary>
    /// Checksum of <see cref="Value"/>.
    /// </summary>
    public readonly Checksum Checksum;

    public readonly object? Value;

    public SolutionAsset(Checksum checksum, object value)
    {
        var kind = value.GetWellKnownSynchronizationKind();

        Checksum = checksum;
        Kind = kind;
        Value = value;
    }
}
