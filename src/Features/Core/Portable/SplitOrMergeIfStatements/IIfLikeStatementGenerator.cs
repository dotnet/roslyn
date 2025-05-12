// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

/// <summary>
/// When querying the syntax, C# else if chains are "flattened" and modeled to look like VB else-if clauses,
/// so an "ifOrElseIf" can be followed a sequence of else-if clauses and an optional final else clause.
/// These else-if clauses are treated as independent when removing or inserting.
/// </summary>
internal interface IIfLikeStatementGenerator : ILanguageService
{
    bool IsIfOrElseIf(SyntaxNode node);

    bool IsCondition(SyntaxNode expression, out SyntaxNode ifOrElseIf);

    bool IsElseIfClause(SyntaxNode node, out SyntaxNode parentIfOrElseIf);

    bool HasElseIfClause(SyntaxNode ifOrElseIf, out SyntaxNode elseIfClause);

    SyntaxNode GetCondition(SyntaxNode ifOrElseIf);

    /// <summary>
    /// Returns the topmost if statement for an else-if clause.
    /// </summary>
    SyntaxNode GetRootIfStatement(SyntaxNode ifOrElseIf);

    /// <summary>
    /// Returns the list of subsequent else-if clauses and a final else clause (if present).
    /// </summary>
    ImmutableArray<SyntaxNode> GetElseIfAndElseClauses(SyntaxNode ifOrElseIf);

    SyntaxNode WithCondition(SyntaxNode ifOrElseIf, SyntaxNode condition);

    SyntaxNode WithStatementInBlock(SyntaxNode ifOrElseIf, SyntaxNode statement);

    SyntaxNode WithStatementsOf(SyntaxNode ifOrElseIf, SyntaxNode otherIfOrElseIf);

    SyntaxNode WithElseIfAndElseClausesOf(SyntaxNode ifStatement, SyntaxNode otherIfStatement);

    /// <summary>
    /// Converts an else-if clause to an if statement, preserving its subsequent else-if and else clauses.
    /// </summary>
    SyntaxNode ToIfStatement(SyntaxNode ifOrElseIf);

    /// <summary>
    /// Convert an if statement to an else-if clause, discarding any of its else-if and else clauses.
    /// </summary>
    SyntaxNode ToElseIfClause(SyntaxNode ifOrElseIf);

    /// <summary>
    /// Inserts <paramref name="elseIfClause"/> as a new else-if clause directly below
    /// <paramref name="afterIfOrElseIf"/>, between it and any of its existing else-if clauses.
    /// </summary>
    void InsertElseIfClause(SyntaxEditor editor, SyntaxNode afterIfOrElseIf, SyntaxNode elseIfClause);

    /// <summary>
    /// Removes <paramref name="elseIfClause"/> from a sequence of else-if clauses, preserving any subsequent clauses.
    /// </summary>
    void RemoveElseIfClause(SyntaxEditor editor, SyntaxNode elseIfClause);
}
