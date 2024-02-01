// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct EncClosureInfo(ClosureDebugInfo debugInfo, DebugId? parentDebugId, ImmutableArray<string> structCaptures)
{
    /// <summary>
    /// Info to write to the PDB.
    /// </summary>
    public readonly ClosureDebugInfo DebugInfo = debugInfo;

    /// <summary>
    /// Id of the parent closure. Only relevant when emitting EnC delta.
    /// </summary>
    public readonly DebugId? ParentDebugId = parentDebugId;

    /// <summary>
    /// Metadata names of fields of a struct closure that store variables captured by the closure. Null for class closures.
    /// Only relevant when emitting EnC delta.
    /// </summary>
    public readonly ImmutableArray<string> StructCaptures = structCaptures;
}
