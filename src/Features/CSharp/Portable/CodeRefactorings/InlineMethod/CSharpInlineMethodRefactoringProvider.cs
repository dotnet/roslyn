// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    internal sealed class CSharpInlineMethodRefactoringProvider : AbstractInlineMethodRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider()
        {
        }

        protected override async Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context)
        {
            var syntaxNode = await context.TryGetRelevantNodeAsync<InvocationExpressionSyntax>().ConfigureAwait(false);
            return syntaxNode;
        }

        private static bool ShouldStatementBeInlined(StatementSyntax statementSyntax)
            => statementSyntax is ReturnStatementSyntax || statementSyntax is ExpressionStatementSyntax || statementSyntax is ThrowStatementSyntax;

        protected override bool IsMethodContainsOneStatement(SyntaxNode methodDeclarationSyntaxNode)
        {
            if (methodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
            {
                var blockSyntaxNode = declarationSyntax.Body;
                // 1. If it is an ordinary method with block
                if (blockSyntaxNode != null)
                {
                    var blockStatements = blockSyntaxNode.Statements;
                    return blockStatements.Count == 1 && ShouldStatementBeInlined(blockStatements[0]);
                }
                else
                {
                    // 2. If it is an Arrow Expression
                    var arrowExpressionNodes = declarationSyntax
                        .DescendantNodes().Where(node => node.IsKind(SyntaxKind.ArrowExpressionClause)).ToImmutableArray();
                    return arrowExpressionNodes.Length == 1;
                }
            }

            return false;
        }

        protected override SyntaxNode ExtractExpressionFromMethodDeclaration(SyntaxNode methodDeclarationSyntax)
        {
            SyntaxNode? inlineSyntaxNode = null;
            if (methodDeclarationSyntax is MethodDeclarationSyntax declarationSyntax)
            {
                var blockSyntaxNode = declarationSyntax.Body;
                // 1. If it is a ordinary method with block
                if (blockSyntaxNode != null)
                {
                    var blockStatements = blockSyntaxNode.Statements;
                    if (blockStatements.Count == 1)
                    {
                        inlineSyntaxNode = GetExpressionFromStatementSyntaxNode(blockStatements[0]);

                    }
                }
                else
                {
                    // 2. If it is using Arrow Expression
                    var arrowExpressionNodes = declarationSyntax
                        .DescendantNodes().Where(node => node.IsKind(SyntaxKind.ArrowExpressionClause)).ToImmutableArray();
                    if (arrowExpressionNodes.Length == 1)
                    {
                        inlineSyntaxNode = ((ArrowExpressionClauseSyntax)arrowExpressionNodes[0]).Expression;
                    }
                }
            }

            return inlineSyntaxNode ??= SyntaxFactory.EmptyStatement();
        }

        private static SyntaxNode? GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                ThrowStatementSyntax throwExpressionSyntax => throwExpressionSyntax.Expression,
                _ => null
            };

        protected override SyntaxNode ReplaceParametersAndGenericsInMethodDeclaration(SyntaxNode methodDeclarationSyntaxNode, SyntaxNode methodInvocationSyntaxNode, IMethodSymbol methodSymbol, SemanticModel semanticModel)
        {
            /*
            TODO:
            Replace parameters & generics in the method body.
            Example:
            Before:
            void Caller(int x)
            {
                Callee<int>(x, k: "Hello");
            }

            void Callee<T>(int i, int j = 0, string k = null)
            {
                Print(typeof(T) + i + j + k);
            }

            After:
            void Caller(int x)
            {
                Callee<int>(x, k: "Hello");
            }

            void Callee<T>(int x, int j = 0, string k = null)
            {
                Print(typeof(int) + x + 0 + "Hello");
            }
            
            Note: here we won't have naming conflict because there is only one statement. There is no statement to declare new local
            variable in the Callee.
             */
            return methodDeclarationSyntaxNode;
        }

        protected override bool TryGetVariableDeclarationsForOutParameters(SyntaxNode methodInvovation, SemanticModel semanticModel, out ImmutableArray<SyntaxNode> variableDeclarations)
        {
            /*
            TODO:
            Genereate local varaible for 'out var' parameters
            Example:

            Before:
            void Caller()
            {
                int i = 10;
                Callee(out i, out var k);
            }

            void Callee(out int i, out int j)
            {
               // Method body
            }

            After:
            void Caller()
            {
                int i = 10;
                int k;
                // Method body
            }
            */
            variableDeclarations = default;
            return false;
        }
    }
}
