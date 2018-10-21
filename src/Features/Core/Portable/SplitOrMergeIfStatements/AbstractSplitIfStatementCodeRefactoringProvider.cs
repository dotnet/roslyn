// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIfStatementCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract string IfKeywordText { get; }

        protected abstract bool IsConditionOfIfStatement(SyntaxNode expression, out SyntaxNode ifStatement);

        protected static bool IsPartOfBinaryExpressionChain(SyntaxToken token, int syntaxKind, out SyntaxNode rootExpression)
        {
            SyntaxNodeOrToken current = token;

            while (current.Parent?.RawKind == syntaxKind)
            {
                current = current.Parent;
            }

            rootExpression = current.AsNode();
            return current.IsNode;
        }

        protected static (SyntaxNode left, SyntaxNode right) SplitBinaryExpressionChain(
            SyntaxToken token, SyntaxNode rootExpression, ISyntaxFactsService syntaxFacts)
        {
            syntaxFacts.GetPartsOfBinaryExpression(token.Parent, out var parentLeft, out _, out var parentRight);

            // (((a && b) && c) && d) && e
            var left = parentLeft;
            var right = rootExpression.ReplaceNode(token.Parent, parentRight);

            return (left, right);
        }
    }
}
