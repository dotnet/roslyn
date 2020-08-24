// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider :
        AbstractInlineMethodRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, MethodDeclarationSyntax, StatementSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance, CSharpSemanticFactsService.Instance)
        {
        }

        protected override ExpressionSyntax? GetInlineExpression(MethodDeclarationSyntax methodDeclarationSyntax)
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

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol, bool allowVar)
            => symbol.GenerateTypeSyntax(allowVar);

        //// TODO: Use the SyntaxGenerator array initialization when this
        //// https://github.com/dotnet/roslyn/issues/46651 is resolved.
        //protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
        //    => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));

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

        protected override bool TryGetInlineSyntaxNodeAndReplacementNodeForDelegate(
            InvocationExpressionSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            ExpressionSyntax inlineExpressionNode,
            StatementSyntax statementContainsCallee,
            SyntaxGenerator syntaxGenerator,
            out SyntaxNode? inlineSyntaxNode,
            out SyntaxNode? syntaxNodeToReplace)
        {
            inlineSyntaxNode = null;
            syntaxNodeToReplace = null;
            if (statementContainsCallee is LocalDeclarationStatementSyntax localDeclarationSyntax
                && IsVariableInitializerInLocalDeclarationSyntax(calleeInvocationNode, localDeclarationSyntax)
                && localDeclarationSyntax.Declaration.Type.IsVar)
            {
                // Example:
                // Before:
                // void Caller() { var x = Callee(); }
                // Action Callee() { return () => {}; }
                //
                // After inline it should be
                // void Caller() { Action x = () => {};}
                // Action Callee() { return () => {}; }
                // 'var' can't be used for delegate
                inlineSyntaxNode = UseExplicitTypeAndReplaceInitializerForDeclarationSyntax(
                    localDeclarationSyntax,
                    syntaxGenerator,
                    calleeMethodSymbol.ReturnType,
                    calleeInvocationNode,
                    inlineExpressionNode);

                syntaxNodeToReplace = statementContainsCallee;
                return true;
            }

            // Example:
            // Before:
            // void Caller() { var x = Callee()(); }
            // Func<int> Callee() { return () => 1; }
            // After:
            // void Caller() { var x = ((Func<int>)(() => 1))(); }
            // Func<int> Callee() { return () => 1; }
            // Cast expression is needed for lambda
            if (calleeInvocationNode.Parent?.IsKind(SyntaxKind.InvocationExpression) == true)
            {
                inlineSyntaxNode = SyntaxFactory.CastExpression(
                    calleeMethodSymbol.ReturnType.GenerateTypeSyntax(allowVar: false),
                    inlineExpressionNode.Parenthesize()).Parenthesize();
                syntaxNodeToReplace = calleeInvocationNode;
                return true;
            }

            return false;
        }

        private static bool IsVariableInitializerInLocalDeclarationSyntax(
            InvocationExpressionSyntax expressionSyntax,
            LocalDeclarationStatementSyntax statementSyntaxEnclosingCallee)
            => statementSyntaxEnclosingCallee.Declaration.Variables
                .Any(variable => expressionSyntax.Equals(variable?.Initializer?.Value));

        private static LocalDeclarationStatementSyntax UseExplicitTypeAndReplaceInitializerForDeclarationSyntax(
            LocalDeclarationStatementSyntax localDeclarationSyntax,
            SyntaxGenerator syntaxGenerator,
            ITypeSymbol type,
            ExpressionSyntax initializer,
            ExpressionSyntax replacementInitializer)
        {
            var syntaxEditor = new SyntaxEditor(localDeclarationSyntax, syntaxGenerator);
            var typeSyntax = type.GenerateTypeSyntax(allowVar: false);
            syntaxEditor.ReplaceNode(localDeclarationSyntax.Declaration.Type, typeSyntax);
            syntaxEditor.ReplaceNode(initializer, replacementInitializer);
            return (LocalDeclarationStatementSyntax)syntaxEditor.GetChangedRoot();
        }
    }
}
