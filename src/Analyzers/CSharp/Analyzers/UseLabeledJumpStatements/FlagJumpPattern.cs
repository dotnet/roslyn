// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

/// <summary>
/// A recognized <c>bool</c>-flag emulation of a multi-level <c>break</c>/<c>continue</c>.
/// </summary>
internal sealed class FlagJumpPattern
{
    /// <summary>The flag declaration to delete.</summary>
    public required LocalDeclarationStatementSyntax LocalDeclarationStatement { get; init; }

    /// <summary>The outermost loop (the final guard's target) to label and break/continue.</summary>
    public required StatementSyntax LoopStatement { get; init; }

    /// <summary>The chain of guards that test the jump value and break/continue (one per level).</summary>
    public required ImmutableArray<IfStatementSyntax> GuardStatements { get; init; }

    /// <summary>The sites that assign the jump value and break; each break becomes the labeled jump.</summary>
    public required ImmutableArray<(ExpressionStatementSyntax Assignment, BreakStatementSyntax Break)> AssignmentAndBreakSites { get; init; }

    /// <summary>Any resets to the initial value at the top of the target loop to delete (empty when the flag relies on
    /// its declaration being re-initialized each iteration instead).</summary>
    public required ImmutableArray<ExpressionStatementSyntax> ResetStatements { get; init; }

    /// <summary>Whether the final guard is a <c>break</c> (otherwise a <c>continue</c>).</summary>
    public required bool IsBreak { get; init; }

    /// <summary>The initial value of the flag, before it changes to signal a jump.</summary>
    public required bool InitialValue { get; init; }
}
