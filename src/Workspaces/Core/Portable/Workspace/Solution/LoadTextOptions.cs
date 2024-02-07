
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Options used to load <see cref="SourceText"/>.
/// </summary>
public readonly record struct LoadTextOptions
{
    public SourceHashAlgorithm ChecksumAlgorithm { get; }

    public LoadTextOptions(SourceHashAlgorithm checksumAlgorithm)
        => ChecksumAlgorithm = checksumAlgorithm;

    public override string ToString()
        => $"{{ {nameof(ChecksumAlgorithm)}: {ChecksumAlgorithm} }}";
}
