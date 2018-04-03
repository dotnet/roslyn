// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseConditionalExpressionForReturnCodeRefactoringProvider
        : AbstractUseConditionalExpressionForReturnCodeFixProvider<ConditionalExpressionSyntax>
    {
        protected override IFormattingRule GetMultiLineFormattingRule()
            => MultiLineConditionalExpressionFormattingRule.Instance;

        protected override ConditionalExpressionSyntax AddTriviaTo(
            ConditionalExpressionSyntax conditional,
            IEnumerable<SyntaxTrivia> trueTrivia,
            IEnumerable<SyntaxTrivia> falseTrivia)
        {
            return conditional.WithWhenTrue(conditional.WhenTrue.WithTrailingTrivia(trueTrivia))
                              .WithWhenFalse(conditional.WhenFalse.WithTrailingTrivia(falseTrivia));
        }
    }
}
