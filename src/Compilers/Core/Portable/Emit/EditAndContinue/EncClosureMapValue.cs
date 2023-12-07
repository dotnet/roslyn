// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct EncClosureMapValue(DebugId id, DebugId? parentId, ImmutableArray<string> structCaptures)
{
    public DebugId Id { get; } = id;
    public DebugId? ParentId { get; } = parentId;
    public ImmutableArray<string> StructCaptures { get; } = structCaptures;

    public bool IsStructClosure
        => !StructCaptures.IsDefault;

    /// <summary>
    /// True if the closure being built is compatible with the previous one.
    /// </summary>
    /// <returns>
    /// True if
    /// - The parent closure hasn't changed
    /// - Both closures are struct closures or neither is.
    /// - The set of variables captured by the new struct closure <paramref name="structCaptures"/>
    ///   must be a subset of previously captured variables <see cref="StructCaptures"/>
    ///   (the runtime doesn't allow adding fields to structs).
    /// </returns>
    public bool IsCompatibleWith(DebugId? parentClosureId, ImmutableArray<string> structCaptures)
        => ParentId == parentClosureId &&
           StructCaptures.IsDefault == structCaptures.IsDefault &&
           (structCaptures.IsDefault || structCaptures.IsSubsetOf(StructCaptures));
}
