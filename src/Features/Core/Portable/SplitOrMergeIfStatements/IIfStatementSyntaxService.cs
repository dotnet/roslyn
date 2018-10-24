// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    /// <summary>
    /// An if-like statement is either an if statement or an else-if clause.
    /// When querying the syntax, C# else if chains are "flattened" and modeled to look like VB else-if clauses.
    /// </summary>
    internal interface IIfStatementSyntaxService : ILanguageService
    {
        bool IsIfLikeStatement(SyntaxNode node);

        bool IsCondition(SyntaxNode expression, out SyntaxNode ifLikeStatement);

        bool IsElseIfClause(SyntaxNode node, out SyntaxNode parentIfLikeStatement);

        SyntaxNode GetCondition(SyntaxNode ifLikeStatement);

        /// <summary>
        /// Returns the list of subsequent else-if clauses and a final else clause (if present).
        /// </summary>
        ImmutableArray<SyntaxNode> GetElseLikeClauses(SyntaxNode ifLikeStatement);

        SyntaxNode WithCondition(SyntaxNode ifLikeStatement, SyntaxNode condition);

        SyntaxNode WithStatement(SyntaxNode ifLikeStatement, SyntaxNode statement);

        SyntaxNode WithStatementsOf(SyntaxNode ifLikeStatement, SyntaxNode otherIfLikeStatement);

        /// <summary>
        /// Converts an else-if clause to an if statement, preserving its subsequent else-if and else clauses.
        /// </summary>
        SyntaxNode ToIfStatement(SyntaxNode ifLikeStatement);

        /// <summary>
        /// Convert an if statement to an else-if clause, discarding any of its else-if and else clauses.
        /// </summary>
        SyntaxNode ToElseIfClause(SyntaxNode ifLikeStatement);

        void InsertElseIfClause(SyntaxEditor editor, SyntaxNode afterIfLikeStatement, SyntaxNode elseIfClause);

        void RemoveElseIfClause(SyntaxEditor editor, SyntaxNode elseIfClause);
    }
}
