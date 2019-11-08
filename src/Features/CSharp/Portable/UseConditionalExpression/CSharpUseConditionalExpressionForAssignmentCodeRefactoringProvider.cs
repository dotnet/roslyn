// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseConditionalExpressionForAssignmentCodeRefactoringProvider
        : AbstractUseConditionalExpressionForAssignmentCodeFixProvider<
            StatementSyntax, IfStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ExpressionSyntax, ConditionalExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpUseConditionalExpressionForAssignmentCodeRefactoringProvider()
        {
        }

        protected override AbstractFormattingRule GetMultiLineFormattingRule()
            => MultiLineConditionalExpressionFormattingRule.Instance;

        protected override VariableDeclaratorSyntax WithInitializer(VariableDeclaratorSyntax variable, ExpressionSyntax value)
            => variable.WithInitializer(SyntaxFactory.EqualsValueClause(value));

        protected override VariableDeclaratorSyntax GetDeclaratorSyntax(IVariableDeclaratorOperation declarator)
            => (VariableDeclaratorSyntax)declarator.Syntax;

        protected override LocalDeclarationStatementSyntax AddSimplificationToType(LocalDeclarationStatementSyntax statement)
            => statement.WithDeclaration(statement.Declaration.WithType(
                statement.Declaration.Type.WithAdditionalAnnotations(Simplifier.Annotation)));

        protected override StatementSyntax WrapWithBlockIfAppropriate(
            IfStatementSyntax ifStatement, StatementSyntax statement)
        {
            if (ifStatement is
            {
                Parent: ElseClauseSyntax _,
                Statement: BlockSyntax { } block
            }
)
            {
                return block.WithStatements(SyntaxFactory.SingletonList(statement))
                            .WithAdditionalAnnotations(Formatter.Annotation);
            }

            return statement;
        }
    }
}
