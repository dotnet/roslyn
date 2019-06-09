// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.InvertLogical;

namespace Microsoft.CodeAnalysis.CSharp.InvertLogical
{
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertLogical), Shared]
    internal class CSharpInvertLogicalCodeRefactoringProvider :
        AbstractInvertLogicalCodeRefactoringProvider<SyntaxKind, ExpressionSyntax, BinaryExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpInvertLogicalCodeRefactoringProvider()
        {
        }

        protected override SyntaxKind GetKind(int rawKind)
            => (SyntaxKind)rawKind;

        protected override SyntaxKind InvertedKind(SyntaxKind binaryExprKind)
            => binaryExprKind == SyntaxKind.LogicalAndExpression
                ? SyntaxKind.LogicalOrExpression
                : SyntaxKind.LogicalAndExpression;

        protected override string GetOperatorText(SyntaxKind binaryExprKind)
            => binaryExprKind == SyntaxKind.LogicalAndExpression
                ? SyntaxFacts.GetText(SyntaxKind.AmpersandAmpersandToken)
                : SyntaxFacts.GetText(SyntaxKind.BarBarToken);
    }
}
