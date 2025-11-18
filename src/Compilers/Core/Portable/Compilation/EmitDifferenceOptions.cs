// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Emit;

public readonly struct EmitDifferenceOptions()
{
    public static readonly EmitDifferenceOptions Default = new();

    /// <summary>
    /// True to emit FieldRva table entries. The runtime must support this feature.
    /// </summary>
    public bool EmitFieldRva { get; init; }
}
