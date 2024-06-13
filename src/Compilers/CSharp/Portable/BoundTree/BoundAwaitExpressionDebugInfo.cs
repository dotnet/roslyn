// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Debug info associated with <see cref="BoundAwaitExpression"/> to support EnC.
/// </summary>
/// <param name="AwaitId">The id of the generated await.</param>
/// <param name="ReservedStateMachineCount">
/// The number of async state machine states to reserve.
/// 
/// Any time multiple <see cref="BoundAwaitExpression"/>s might be associated with the same syntax node
/// we need to make sure that the same number of state machine states gets allocated for the node,
/// regardless of the actual number of <see cref="BoundAwaitExpression"/>s that get emitted.
/// 
/// To do so one or more of the emitted <see cref="BoundAwaitExpression"/>s may
/// reserve additional dummy state machine states so that the total number of states
/// (one for each <see cref="BoundAwaitExpression"/> plus total reserved states) is constant
/// regardless of semantics of the syntax node.
/// 
/// E.g. `await foreach` produces at least one and at most two <see cref="BoundAwaitExpression"/>s:
/// one for MoveNextAsync and the other for DisposeAsync.
/// 
/// If the enumerator is async-disposable it produces two <see cref="BoundAwaitExpression"/>s with 
/// <paramref name="ReservedStateMachineCount"/> set to 0.
/// 
/// If the enumerator is not async-disposable it produces a single <see cref="BoundAwaitExpression"/> with 
/// <paramref name="ReservedStateMachineCount"/> set to 1.
/// 
/// The states are only reserved in DEBUG builds.
/// </param>
internal readonly record struct BoundAwaitExpressionDebugInfo(AwaitDebugId AwaitId, byte ReservedStateMachineCount);
