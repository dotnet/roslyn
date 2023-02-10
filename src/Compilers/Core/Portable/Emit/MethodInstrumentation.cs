// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Emit;

/// <summary>
/// Represents instrumentation of a method.
/// </summary>
public readonly struct MethodInstrumentation
{
    internal static readonly MethodInstrumentation Empty = new MethodInstrumentation()
    {
        Kinds = ImmutableArray<InstrumentationKind>.Empty,
    };

    /// <summary>
    /// Kinds of instrumentation to apply to the entire method body.
    /// </summary>
    public ImmutableArray<InstrumentationKind> Kinds { get; init; }

    internal bool IsDefault
        => Kinds.IsDefault;

    internal bool IsEmpty
        => Kinds.IsEmpty;
}
