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
    /// <summary>The <c>bool flag = false;</c> declaration to delete.</summary>
    public required LocalDeclarationStatementSyntax LocalDeclarationStatement { get; init; }

    /// <summary>The outer loop to label and break/continue.</summary>
    public required StatementSyntax LoopStatement { get; init; }

    /// <summary>The <c>if (flag) break;</c>/<c>if (flag) continue;</c> guard to delete.</summary>
    public required IfStatementSyntax GuardStatement { get; init; }

    /// <summary>The inner <c>flag = true; break;</c> sites; each break becomes the labeled jump.</summary>
    public required ImmutableArray<(ExpressionStatementSyntax Assignment, BreakStatementSyntax Break)> Sites { get; init; }

    /// <summary>Whether the guard is a <c>break</c> (otherwise a <c>continue</c>).</summary>
    public required bool IsBreak { get; init; }
}
