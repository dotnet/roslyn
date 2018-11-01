// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    /// <summary>
    /// An if-like statement is either an if statement or an else-if clause.
    /// When querying the syntax, C# else if chains are "flattened" and modeled to look like VB else-if clauses,
    /// so an if-like statement can be followed a sequence of else-if clauses (which are themselves if-like statements)
    /// and an optional final else clause. These else-if clauses are treated as independent when removing or inserting.
    /// </summary>
    internal interface IIfLikeStatementGenerator : ILanguageService
    {
        bool IsIfLikeStatement(SyntaxNode node);

        bool IsCondition(SyntaxNode expression, out SyntaxNode ifLikeStatement);

        bool IsElseIfClause(SyntaxNode node, out SyntaxNode parentIfLikeStatement);

        SyntaxNode GetCondition(SyntaxNode ifLikeStatement);

        /// <summary>
        /// Returns the topmost if statement for an else-if clause.
        /// </summary>
        SyntaxNode GetRootIfStatement(SyntaxNode ifLikeStatement);

        /// <summary>
        /// Returns the list of subsequent else-if clauses and a final else clause (if present).
        /// </summary>
        ImmutableArray<SyntaxNode> GetElseLikeClauses(SyntaxNode ifLikeStatement);

        SyntaxNode WithCondition(SyntaxNode ifLikeStatement, SyntaxNode condition);

        SyntaxNode WithStatementInBlock(SyntaxNode ifLikeStatement, SyntaxNode statement);

        SyntaxNode WithStatementsOf(SyntaxNode ifLikeStatement, SyntaxNode otherIfLikeStatement);

        SyntaxNode WithElseLikeClausesOf(SyntaxNode ifStatement, SyntaxNode otherIfStatement);

        /// <summary>
        /// Converts an else-if clause to an if statement, preserving its subsequent else-if and else clauses.
        /// </summary>
        SyntaxNode ToIfStatement(SyntaxNode ifLikeStatement);

        /// <summary>
        /// Convert an if statement to an else-if clause, discarding any of its else-if and else clauses.
        /// </summary>
        SyntaxNode ToElseIfClause(SyntaxNode ifLikeStatement);

        /// <summary>
        /// Inserts <paramref name="elseIfClause"/> as a new else-if clause directly below
        /// <paramref name="afterIfLikeStatement"/>, between it and any of its existing else-if clauses.
        /// </summary>
        void InsertElseIfClause(SyntaxEditor editor, SyntaxNode afterIfLikeStatement, SyntaxNode elseIfClause);

        /// <summary>
        /// Removes <paramref name="elseIfClause"/> from a sequence of else-if clauses, preserving any subsequent clauses.
        /// </summary>
        void RemoveElseIfClause(SyntaxEditor editor, SyntaxNode elseIfClause);
    }
}
