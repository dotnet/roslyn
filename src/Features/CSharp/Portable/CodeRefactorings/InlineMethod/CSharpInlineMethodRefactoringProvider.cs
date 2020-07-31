// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    internal sealed class CSharpInlineMethodRefactoringProvider : AbstractInlineMethodRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance)
        {
        }

        protected override async Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context)
        {
            var syntaxNode = await context.TryGetRelevantNodeAsync<InvocationExpressionSyntax>().ConfigureAwait(false);
            return syntaxNode;
        }

        private static bool ShouldStatementBeInlined(StatementSyntax statementSyntax)
            => statementSyntax is ReturnStatementSyntax || statementSyntax is ExpressionStatementSyntax || statementSyntax is ThrowStatementSyntax;

        protected override bool IsMethodContainsOneStatement(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            if (calleeMethodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
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

        protected override IParameterSymbol? GetParameterSymbol(SemanticModel semanticModel, SyntaxNode argumentSyntaxNode, CancellationToken cancellationToken)
            => argumentSyntaxNode is ArgumentSyntax argumentSyntax
                ? argumentSyntax.DetermineParameter(semanticModel, allowParams: true, cancellationToken)
                : null;

        protected override bool IsExpressionStatement(SyntaxNode syntaxNode)
            => syntaxNode.IsKind(SyntaxKind.ExpressionStatement);

        protected override SyntaxNode GenerateLiteralExpression(ITypeSymbol typeSymbol, object? value)
            => ExpressionGenerator.GenerateExpression(typeSymbol, value, canUseFieldReference: false);

        protected override bool IsExpressionSyntax(SyntaxNode syntaxNode)
            => syntaxNode is ExpressionSyntax;

        protected override string GetIdentifierTokenTextFromIdentifierNameSyntax(SyntaxNode syntaxNode)
        {
            if (syntaxNode is IdentifierNameSyntax identifierNameSyntax)
            {
                return identifierNameSyntax.Identifier.ValueText;
            }

            return string.Empty;
        }

        protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));

        protected override SyntaxNode GenerateLocalDeclarationStatementWithRightHandExpression(
            string identifierTokenName,
            ITypeSymbol type,
            SyntaxNode expression)
            => SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    type.GenerateTypeSyntax(),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(identifierTokenName),
                            argumentList: null,
                            initializer: SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression)))));

        protected override SyntaxNode GenerateLocalDeclarationStatement(string identifierTokenName, ITypeSymbol type)
            => SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    type.GenerateTypeSyntax(),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(identifierTokenName))));

        protected override SyntaxNode GenerateIdentifierNameSyntaxNode(string name)
            => SyntaxFactory.IdentifierName(name);

        protected override SyntaxNode GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode, bool shouldGenerateTempVariableForReturnValue)
        {
            SyntaxNode? inlineSyntaxNode = null;
            if (calleeMethodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
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

        protected override bool IsEmbeddedStatementOwner(SyntaxNode syntaxNode)
            => syntaxNode.IsEmbeddedStatementOwner();

        protected override bool IsArrayCreationExpressionOrImplicitArrayCreationExpression(SyntaxNode syntaxNode)
            => syntaxNode.IsKind(SyntaxKind.ArrayCreationExpression) || syntaxNode.IsKind(SyntaxKind.ImplicitArrayCreationExpression);
    }
}
