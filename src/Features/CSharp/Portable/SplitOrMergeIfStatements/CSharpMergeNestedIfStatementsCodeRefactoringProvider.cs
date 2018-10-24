// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), Shared]
    internal sealed class CSharpMergeNestedIfStatementsCodeRefactoringProvider
        : AbstractMergeNestedIfStatementsCodeRefactoringProvider
    {
        protected override bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifLikeStatement)
        {
            if (node is IfStatementSyntax ifStatement)
            {
                // Cases:
                // 1. Position is at a direct token child of an if statement with no selection (e.g. 'if' keyword, a parenthesis)
                // 2. Selection around the 'if' keyword
                // 3. Selection around the header - from 'if' keyword to the end of the condition
                // 4. Selection around the whole if statement
                if (span.Length == 0 ||
                    span.IsAround(ifStatement.IfKeyword) ||
                    span.IsAround(ifStatement.IfKeyword, ifStatement.CloseParenToken) ||
                    span.IsAround(node))
                {
                    ifLikeStatement = ifStatement;
                    return true;
                }
            }

            ifLikeStatement = null;
            return false;
        }
    }
}
