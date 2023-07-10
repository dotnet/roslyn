
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Options used to load <see cref="SourceText"/>.
/// </summary>
public readonly struct LoadTextOptions(SourceHashAlgorithm checksumAlgorithm) : IEquatable<LoadTextOptions>
{
    public SourceHashAlgorithm ChecksumAlgorithm { get; } = checksumAlgorithm;

    public bool Equals(LoadTextOptions other)
        => ChecksumAlgorithm == other.ChecksumAlgorithm;

    public override bool Equals(object? obj)
        => obj is LoadTextOptions options && Equals(options);

    public static bool operator ==(LoadTextOptions left, LoadTextOptions right)
        => left.Equals(right);

    public static bool operator !=(LoadTextOptions left, LoadTextOptions right)
        => !(left == right);

    public override int GetHashCode()
        => ((int)ChecksumAlgorithm).GetHashCode();

    public override string ToString()
        => $"{{ {nameof(ChecksumAlgorithm)}: {ChecksumAlgorithm} }}";
}
