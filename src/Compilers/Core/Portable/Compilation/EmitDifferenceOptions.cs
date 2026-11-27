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

    /// <summary>
    /// True if the runtime supports adding entries to MethodImpl table (i.e. adding explicit method implementations in the delta).
    /// Some runtimes (.NET Framework) do not support this feature.
    /// </summary>
    public bool MethodImplEntriesSupported { get; init; } = true;
}
