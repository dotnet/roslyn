// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider :
        AbstractInlineMethodRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax>
    {
        /// <summary>
        /// All the syntax kind considered as the statement contains the invocation callee.
        /// </summary>
        private static readonly ImmutableHashSet<SyntaxKind> s_syntaxKindsConsideredAsStatementInvokesCallee =
            ImmutableHashSet.Create(
                SyntaxKind.DoStatement,
                SyntaxKind.ExpressionStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.IfStatement,
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.LockStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.SwitchStatement,
                SyntaxKind.ThrowStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.TryStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.YieldReturnStatement);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance, CSharpSemanticFactsService.Instance)
        {
        }

        private static bool CanStatementBeInlined(StatementSyntax statementSyntax)
        {
            if (statementSyntax.IsKind(SyntaxKind.ExpressionStatement))
            {
                return true;
            }

            if (statementSyntax is ReturnStatementSyntax returnStatementSyntax)
            {
                // In this case don't provide inline.
                // void Caller() { Callee(); }
                // void Callee() { return; }
                return returnStatementSyntax.Expression != null;
            }

            return false;
        }

        protected override bool IsSingleStatementOrExpressionMethod(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            if (calleeMethodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
            {
                var blockSyntaxNode = declarationSyntax.Body;
                // 1. If it is an ordinary method with block
                if (blockSyntaxNode != null)
                {
                    var blockStatements = blockSyntaxNode.Statements;
                    return blockStatements.Count == 1 && CanStatementBeInlined(blockStatements[0]);
                }
                else
                {
                    // 2. If it is an Arrow Expression
                    var arrowExpressionNodes = declarationSyntax.ExpressionBody;
                    return arrowExpressionNodes != null;
                }
            }

            return false;
        }

        protected override IParameterSymbol? GetParameterSymbol(SemanticModel semanticModel, ArgumentSyntax argumentSyntaxNode, CancellationToken cancellationToken)
            => argumentSyntaxNode.DetermineParameter(semanticModel, allowParams: true, cancellationToken);

        protected override ExpressionSyntax GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            var declarationSyntax = (MethodDeclarationSyntax)calleeMethodDeclarationSyntaxNode;
            var blockSyntaxNode = declarationSyntax.Body;
            // Check has been done before to make sure block statements only has one statement
            // or the declarationSyntax has an ExpressionBody.
            if (blockSyntaxNode != null)
            {
                // 1. If it is an ordinary method with block
                var blockStatements = blockSyntaxNode.Statements;
                return GetExpressionFromStatementSyntaxNode(blockStatements[0]);
            }
            else
            {
                // 2. If it is using Arrow Expression
                var arrowExpressionNode = declarationSyntax.ExpressionBody;
                return arrowExpressionNode!.Expression;
            }
        }

        private static ExpressionSyntax GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                // Check has been done before to make sure the argument is ReturnStatementSyntax or ExpressionStatementSyntax
                // and their expression is not null
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression!,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                _ => throw ExceptionUtilities.Unreachable
            };

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol)
            => symbol.GenerateTypeSyntax(allowVar: false);

        protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));

        protected override bool IsStatementConsideredAsInvokingStatement(SyntaxNode node)
            => s_syntaxKindsConsideredAsStatementInvokesCallee.Contains(node.Kind());

        protected override ExpressionSyntax Parenthesize(ExpressionSyntax expressionSyntax)
            => expressionSyntax.Parenthesize();
    }
}
