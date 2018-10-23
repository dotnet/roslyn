// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportLanguageService(typeof(IIfStatementSyntaxService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpIfStatementSyntaxService : IIfStatementSyntaxService
    {
        public int IfKeywordKind => (int)SyntaxKind.IfKeyword;

        public int LogicalAndExpressionKind => (int)SyntaxKind.LogicalAndExpression;

        public int LogicalOrExpressionKind => (int)SyntaxKind.LogicalOrExpression;

        public bool IsConditionOfIfLikeStatement(SyntaxNode expression, out SyntaxNode ifLikeStatement)
        {
            if (expression.Parent is IfStatementSyntax ifStatement && ifStatement.Condition == expression)
            {
                ifLikeStatement = ifStatement;
                return true;
            }

            ifLikeStatement = null;
            return false;
        }

        public ImmutableArray<SyntaxNode> GetElseLikeClauses(SyntaxNode ifLikeStatement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;

            return ImmutableArray.Create<SyntaxNode>(ifStatement.Else);
        }
    }
}
