// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Tracks the reachability of the end of a bound node that either already is a try/finally, or has the potential to become one during lowering.
/// Only nodes that can have an await in the finally block are tracked with this enum, for use in <see cref="AsyncExceptionHandlerRewriter.VisitTryStatement(BoundTryStatement)"/>.
/// Try/finally nodes that are synthesized by the compiler during lowering and do not have an await in the finally block can use <see cref="AsyncTryFinallyEndReachable.Ignored"/>.
/// </summary>
internal enum AsyncTryFinallyEndReachable
{
    /// <summary>
    /// The reachability of the end of the node has not been determined yet.
    /// </summary>
    Unknown,

    /// <summary>
    /// The end of the node is reachable.
    /// </summary>
    Reachable,

    /// <summary>
    /// The end of the node is unreachable.
    /// </summary>
    Unreachable,

    /// <summary>
    /// The reachability of the end of the node is intentionally not set. This should never be encountered in a try/finally that has an await in the finally block.
    /// </summary>
    Ignored
}
