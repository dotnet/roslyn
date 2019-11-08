// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseConditionalExpressionForReturnCodeRefactoringProvider
        : AbstractUseConditionalExpressionForReturnCodeFixProvider<StatementSyntax, IfStatementSyntax, ExpressionSyntax, ConditionalExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpUseConditionalExpressionForReturnCodeRefactoringProvider()
        {
        }

        protected override bool IsRef(IReturnOperation returnOperation)
            => returnOperation is
        {
            Syntax: ReturnStatementSyntax { Expression: RefExpressionSyntax _ } statement
        };

        protected override AbstractFormattingRule GetMultiLineFormattingRule()
            => MultiLineConditionalExpressionFormattingRule.Instance;

        protected override StatementSyntax WrapWithBlockIfAppropriate(
            IfStatementSyntax ifStatement, StatementSyntax statement)
        {
            if (ifStatement.Parent is ElseClauseSyntax &&
                ifStatement.Statement is BlockSyntax block)
            {
                return block.WithStatements(SyntaxFactory.SingletonList(statement))
                            .WithAdditionalAnnotations(Formatter.Annotation);
            }

            return statement;
        }
    }
}
