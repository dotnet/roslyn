// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal interface IIfStatementSyntaxService : ILanguageService
    {
        int IfKeywordKind { get; }

        int LogicalAndExpressionKind { get; }

        int LogicalOrExpressionKind { get; }

        bool IsIfLikeStatement(SyntaxNode node);

        bool IsConditionOfIfLikeStatement(SyntaxNode expression, out SyntaxNode ifLikeStatement);

        bool IsElseIfClause(SyntaxNode node, out SyntaxNode parentIfLikeStatement);

        SyntaxNode GetConditionOfIfLikeStatement(SyntaxNode ifLikeStatement);

        ImmutableArray<SyntaxNode> GetElseLikeClauses(SyntaxNode ifLikeStatement);

        SyntaxNode WithCondition(SyntaxNode ifOrElseIfNode, SyntaxNode condition);

        SyntaxNode WithStatement(SyntaxNode ifOrElseIfNode, SyntaxNode statement);

        SyntaxNode WithStatementsOf(SyntaxNode ifOrElseIfNode, SyntaxNode otherIfOrElseIfNode);

        SyntaxNode ToIfStatement(SyntaxNode ifOrElseIfNode);

        SyntaxNode ToElseIfClause(SyntaxNode ifOrElseIfNode);

        void InsertElseIfClause(SyntaxEditor editor, SyntaxNode ifOrElseIfNode, SyntaxNode elseIfClause);

        void RemoveElseIfClause(SyntaxEditor editor, SyntaxNode elseIfClause);
    }
}
