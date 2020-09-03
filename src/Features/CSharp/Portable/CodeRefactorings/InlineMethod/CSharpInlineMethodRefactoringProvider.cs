// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider :
        AbstractInlineMethodRefactoringProvider<BaseMethodDeclarationSyntax, StatementSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider()
            : base(CSharpSyntaxFacts.Instance, CSharpSemanticFactsService.Instance)
        {
        }

        protected override ExpressionSyntax? GetRawInlineExpression(BaseMethodDeclarationSyntax methodDeclarationSyntax)
        {
            var blockSyntaxNode = methodDeclarationSyntax.Body;
            if (blockSyntaxNode != null)
            {
                // 1. If it is an ordinary method with block
                var blockStatements = blockSyntaxNode.Statements;
                if (blockStatements.Count == 1)
                {
                    var statementSyntax = blockStatements[0];
                    return statementSyntax switch
                    {
                        // Note: For this case this will return null in Callee()
                        // void Caller() { Callee(); }
                        // void Callee() { return; }
                        // Refactoring won't be provided for this case.
                        ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression,
                        ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                        ThrowStatementSyntax throwStatementSyntax => throwStatementSyntax.Expression,
                        _ => null
                    };
                }
            }
            else
            {
                // 2. If it is an Arrow Expression
                var arrowExpressionNode = methodDeclarationSyntax.ExpressionBody;
                return arrowExpressionNode?.Expression;
            }

            return null;
        }

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol, bool allowVar)
            => symbol.GenerateTypeSyntax(allowVar);

        protected override ExpressionSyntax GenerateLiteralExpression(ITypeSymbol typeSymbol, object? value)
            => ExpressionGenerator.GenerateExpression(typeSymbol, value, canUseFieldReference: true);

        protected override bool IsFieldDeclarationSyntax(SyntaxNode node)
            => node.IsKind(SyntaxKind.FieldDeclaration);

        protected override bool IsValidExpressionUnderExpressionStatement(ExpressionSyntax expressionNode)
        {
            // C# Expression Statements defined in the language reference
            // expression_statement
            //     : statement_expression ';'
            //     ;
            //
            // statement_expression
            //     : invocation_expression
            //     | null_conditional_invocation_expression
            //     | object_creation_expression
            //     | assignment
            //     | post_increment_expression
            //     | post_decrement_expression
            //     | pre_increment_expression
            //     | pre_decrement_expression
            //     | await_expression
            //     ;
            var isNullConditionalInvocationExpression = IsNullConditionalInvocationExpression(expressionNode);

            return expressionNode.IsKind(SyntaxKind.InvocationExpression)
                   || isNullConditionalInvocationExpression
                   || expressionNode.IsKind(SyntaxKind.ObjectCreationExpression)
                   || expressionNode is AssignmentExpressionSyntax
                   || expressionNode.IsKind(SyntaxKind.PreIncrementExpression)
                   || expressionNode.IsKind(SyntaxKind.PreDecrementExpression)
                   || expressionNode.IsKind(SyntaxKind.PostIncrementExpression)
                   || expressionNode.IsKind(SyntaxKind.PostDecrementExpression)
                   || expressionNode.IsKind(SyntaxKind.AwaitExpression);
        }

        protected override bool CanBeReplacedByThrowExpression(SyntaxNode syntaxNode)
        {
            // C# Throw Expression definition in language reference:
            // 'A throw expression is permitted in only the following syntactic contexts:
            // As the second or third operand of a ternary conditional operator ?:
            // As the second operand of a null coalescing operator ??
            // As the body of an expression-bodied lambda or method.'
            var parent = syntaxNode.Parent;
            if (parent is ConditionalExpressionSyntax conditionalExpressionSyntax)
            {
                return syntaxNode.Equals(conditionalExpressionSyntax.WhenTrue)
                    || syntaxNode.Equals(conditionalExpressionSyntax.WhenFalse);
            }

            if (parent is BinaryExpressionSyntax binaryExpressionSyntax && binaryExpressionSyntax.IsKind(SyntaxKind.CoalesceExpression))
            {
                return syntaxNode.Equals(binaryExpressionSyntax.Right);
            }

            if (parent is LambdaExpressionSyntax lambdaExpressionSyntax)
            {
                return lambdaExpressionSyntax.ExpressionBody != null;
            }

            return parent.IsKind(SyntaxKind.ArrowExpressionClause);
        }

        private static bool IsNullConditionalInvocationExpression(ExpressionSyntax expressionSyntax)
        {
            // Check if the expression syntax is like an invocation expression nested inside ConditionalAccessExpressionSyntax.
            // For example: a?.b.c()
            if (expressionSyntax is ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
            {
                var whenNotNull = conditionalAccessExpressionSyntax.WhenNotNull;
                // If the expression is ended with an invocation
                // (if the expressions in the middle are not ConditionalAccessExpressionSyntax),
                // like a?.b.e.c(), the syntax tree would be
                // ConditionalAccessExpressionSyntax -> InvocationExpression.
                // And in case of example like a?.b?.d?.c();
                // This is case it would be
                // ConditionalAccessExpressionSyntax -> ConditionalAccessExpressionSyntax -> ... -> InvocationExpression.
                return whenNotNull.IsKind(SyntaxKind.InvocationExpression) || IsNullConditionalInvocationExpression(whenNotNull);
            }

            return false;
        }
    }
}
