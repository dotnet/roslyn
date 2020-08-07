// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider : CodeRefactoringProvider
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly IPrecedenceService _precedenceService;

        protected abstract Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Check if the <param name="calleeMethodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsMethodContainsOneStatement(SyntaxNode calleeMethodDeclarationSyntaxNode);

        protected abstract SyntaxNode? GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode);

        protected AbstractInlineMethodRefactoringProvider(ISyntaxFacts syntaxFacts, IPrecedenceService precedenceService)
        {
            _syntaxFacts = syntaxFacts;
            _precedenceService = precedenceService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationSyntaxNode = await GetInvocationExpressionSyntaxNodeAsync(context).ConfigureAwait(false);
            if (calleeMethodInvocationSyntaxNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetSymbolInfo(calleeMethodInvocationSyntaxNode, cancellationToken).GetAnySymbol();
            if (methodSymbol == null
                || methodSymbol.DeclaredAccessibility != Accessibility.Private
                || !methodSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (methodSymbol is IMethodSymbol calleeMethodInvocationSymbol)
            {
                var calleeMethodDeclarationSyntaxNodes = await Task.WhenAll(calleeMethodInvocationSymbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntaxAsync())).ConfigureAwait(false);

                if (calleeMethodDeclarationSyntaxNodes == null || calleeMethodDeclarationSyntaxNodes.Length != 1)
                {
                    return;
                }

                var calleeMethodDeclarationSyntaxNode = calleeMethodDeclarationSyntaxNodes[0];

                if (!IsMethodContainsOneStatement(calleeMethodDeclarationSyntaxNode))
                {
                    return;
                }

                var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                if (root == null)
                {
                    return;
                }

                var codeAction = new CodeAction.DocumentChangeAction(
                    string.Format(FeaturesResources.Inline_0, calleeMethodInvocationSymbol.ToNameDisplayString()),
                    cancellationToken => InlineMethodAsync(
                        document,
                        calleeMethodInvocationSyntaxNode,
                        calleeMethodInvocationSymbol,
                        calleeMethodDeclarationSyntaxNode,
                        cancellationToken));

                context.RegisterRefactoring(codeAction);
            }
        }

        private async Task<Document> InlineMethodAsync(
            Document document,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodParametersInfo = MethodParametersInfo.GetMethodParametersInfo(
                this,
                _syntaxFacts,
                semanticModel,
                calleeMethodInvocationSyntaxNode,
                calleeMethodSymbol,
                cancellationToken);

            var methodInvocationInfo = MethodInvocationInfo.GetMethodInvocationInfo(
                _syntaxFacts,
                this,
                calleeMethodInvocationSyntaxNode);

            var inlineContext = await InlineMethodContext.GetInlineContextAsync(
                this,
                _syntaxFacts,
                _precedenceService,
                document,
                semanticModel,
                calleeMethodInvocationSyntaxNode,
                calleeMethodSymbol,
                calleeMethodDeclarationSyntaxNode,
                GetInlineStatement(calleeMethodDeclarationSyntaxNode),
                methodParametersInfo,
                methodInvocationInfo,
                cancellationToken).ConfigureAwait(false);

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            foreach (var statement in inlineContext.DeclarationStatementsGenerated)
            {
                documentEditor.InsertBefore(inlineContext.StatementContainsCalleeInvocationExpression, statement);
            }

            var syntaxNodeToReplace = inlineContext.SyntaxNodeToReplace;
            var inlineSyntaxNode = inlineContext.InlineSyntaxNode;
            if (inlineSyntaxNode == null && calleeMethodSymbol.ReturnsVoid)
            {
                // When it has only one return statement in the callee & return void, just remove the whole statement.
                documentEditor.RemoveNode(syntaxNodeToReplace);
            }
            else
            {
                documentEditor.ReplaceNode(syntaxNodeToReplace, inlineSyntaxNode);
            }

            var x = documentEditor.GetChangedDocument();
            return x;
        }
    }
}
