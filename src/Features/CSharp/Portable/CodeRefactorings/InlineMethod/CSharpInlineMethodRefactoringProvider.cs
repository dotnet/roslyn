// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
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
        AbstractInlineMethodRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, MethodDeclarationSyntax, IdentifierNameSyntax, StatementSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance, CSharpSemanticFactsService.Instance)
        {
        }

        private static bool CanStatementBeInlined(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                ExpressionStatementSyntax _ => true,
                // In this case don't provide inline.
                // void Caller() { Callee(); }
                // void Callee() { return; }
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression != null,
                _ => false
            };

        protected override ExpressionSyntax? GetInlineExpression(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var blockSyntaxNode = methodDeclarationSyntax.Body;
            if (blockSyntaxNode != null)
            {
                // 1. If it is an ordinary method with block
                var blockStatements = blockSyntaxNode.Statements;
                if (blockStatements.Count == 1 && CanStatementBeInlined(blockStatements[0]))
                {
                    StatementSyntax statementSyntax = blockStatements[0];
                    return statementSyntax switch
                    {
                        // Check has been done before to make sure the argument is ReturnStatementSyntax or ExpressionStatementSyntax
                        // and their expression is not null
                        ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression!,
                        ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                        _ => null
                    };
                }
            }
            else
            {
                // 2. If it is an Arrow Expression
                var arrowExpressionNode = methodDeclarationSyntax.ExpressionBody;
                if (arrowExpressionNode != null)
                {
                    return arrowExpressionNode.Expression;
                }
            }

            return null;
        }

        protected override SyntaxNode? GetEnclosingMethodLikeNode(SyntaxNode syntaxNode)
        {
            for (var node = syntaxNode; node != null; node = node.Parent)
            {
                if (node.IsKind(SyntaxKind.MethodDeclaration)
                    || node.IsKind(SyntaxKind.LocalFunctionStatement)
                    || node is LambdaExpressionSyntax)
                {
                    return node;
                }
            }

            return null;
        }

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol)
            => symbol.GenerateTypeSyntax();

        // TODO: Use the SyntaxGenerator array initialization when this
        // https://github.com/dotnet/roslyn/issues/46651 is resolved.
        protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));

        protected override ExpressionSyntax Parenthesize(ExpressionSyntax expressionSyntax)
            => expressionSyntax.Parenthesize();

        protected override bool IsValidExpressionUnderStatementExpression(ExpressionSyntax expressionNode)
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
            var isNullConditionalInvocationExpression =
                expressionNode is ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax
                && conditionalAccessExpressionSyntax.WhenNotNull.IsKind(SyntaxKind.InvocationExpression);

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
    }
}
