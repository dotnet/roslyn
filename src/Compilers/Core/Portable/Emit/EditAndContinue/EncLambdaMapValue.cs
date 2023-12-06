// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct EncLambdaMapValue(DebugId id, int closureOrdinal, ImmutableArray<DebugId> structClosureIds)
{
    public readonly DebugId Id = id;
    public readonly int ClosureOrdinal = closureOrdinal;
    public readonly ImmutableArray<DebugId> StructClosureIds = structClosureIds;

    /// <summary>
    /// True if the lambda being built is compatible with the previous one.
    /// </summary>
    /// <returns>
    /// True if
    /// - The closure ordinal of the previous lambda is the same as the current one.
    ///   It is not necessary to check that the generation of the closure matches. 
    ///   
    ///   Two closures of the same ordinal that differ in generation can only exist because the first closure was deleted (or regenerated due to a rude edit) in
    ///   an earlier generation and the latter closure corresponding to the same scope was added in a subsequent generation.
    ///   The above condition ensures that the current lambda syntax maps to an existing lambda syntax in the previous generation.
    ///   The closure the previous lambda is emitted to couldn't have been deleted.
    ///   
    ///   Guarantees that the containing type of the synthesized method remains unchanged.
    /// 
    /// - The sequence of struct closures the local function captures is preserved.
    ///   Guarantees that the signature of the synthesized method remains unchanged.
    /// </returns>
    public bool IsCompatibleWith(int closureOrdinal, ImmutableArray<DebugId> structClosureIds)
        => ClosureOrdinal == closureOrdinal &&
           StructClosureIds.SequenceEqual(structClosureIds);
}
