// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract class AbstractInlineMethodRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Check if the <param name="methodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsMethodContainsOneStatement(SyntaxNode methodDeclarationSyntaxNode);

        /// <summary>
        /// Extract the expression from the single one statement or Arrow Expression in <param name="methodDeclarationSyntaxNode"/>.
        /// </summary>
        protected abstract SyntaxNode ExtractExpressionFromMethodDeclaration(SyntaxNode methodDeclarationSyntaxNode);

        /// <summary>
        /// Replace the parameters of <param name="methodDeclarationSyntaxNode"/> by using the
        /// input parameters from <param name="methodInvocationSyntaxNode"/>
        /// </summary>
        protected abstract SyntaxNode ReplaceParametersAndGenericsInMethodDeclaration(SyntaxNode methodDeclarationSyntaxNode, SyntaxNode methodInvocationSyntaxNode, IMethodSymbol methodSymbol, SemanticModel semanticModel);

        /// <summary>
        /// Generate the 
        /// </summary>
        /// <param name="methodInvovation"></param>
        /// <param name="semanticModel"></param>
        /// <param name="variableDeclarations"></param>
        /// <returns></returns>
        protected abstract bool TryGetVariableDeclarationsForOutParameters(SyntaxNode methodInvovation, SemanticModel semanticModel, out ImmutableArray<SyntaxNode> variableDeclarations);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var methodInvocationNode = await GetInvocationExpressionSyntaxNodeAsync(context).ConfigureAwait(false);
            if (methodInvocationNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            var calleeMethodDeclarationSymbol = semanticModel.GetSymbolInfo(methodInvocationNode).Symbol;

            if (calleeMethodDeclarationSymbol == null
                || calleeMethodDeclarationSymbol.DeclaredAccessibility != Accessibility.Private
                || !calleeMethodDeclarationSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (calleeMethodDeclarationSymbol is IMethodSymbol methodSymbol)
            {
                var methodDeclarationSyntaxNodes = await Task.WhenAll(methodSymbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntaxAsync())).ConfigureAwait(false);

                if (methodDeclarationSyntaxNodes == null || methodDeclarationSyntaxNodes.Length != 1)
                {
                    return;
                }

                var methodDeclarationSyntaxNode = methodDeclarationSyntaxNodes[0];

                if (!IsMethodContainsOneStatement(methodDeclarationSyntaxNode))
                {
                    return;
                }

                var codeAction = new CodeAction.DocumentChangeAction(
                    string.Format(FeaturesResources.Inline_0, methodSymbol.ToNameDisplayString()),
                    cancellationToken => InlineMethodAsync(document, semanticModel!, methodInvocationNode, methodSymbol, methodDeclarationSyntaxNode, cancellationToken));

                context.RegisterRefactoring(codeAction);
            }
        }

        private async Task<Document> InlineMethodAsync(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode methodInvocationSyntaxNode,
            IMethodSymbol methodSymbol,
            SyntaxNode methodDeclarationSyntaxNode,
            CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // 1. Using the input parameter from caller to replace callee's parameter. Because this feature only supports
            // one line scenario now, there won't be any naming conflict.
            var methodDeclarationAfterParameterReplacement = ReplaceParametersAndGenericsInMethodDeclaration(methodDeclarationSyntaxNode, methodInvocationSyntaxNode, methodSymbol, semanticModel);

            // 2. Extract the Expression from the statement.
            var methodStatement = ExtractExpressionFromMethodDeclaration(methodDeclarationAfterParameterReplacement);

            // 3. Generate local variables for out variables.
            if (TryGetVariableDeclarationsForOutParameters(methodInvocationSyntaxNode, semanticModel, out var variableDeclarations))
            {
                documentEditor.InsertBefore(methodInvocationSyntaxNode, variableDeclarations);
            }

            documentEditor.ReplaceNode(methodInvocationSyntaxNode, methodStatement);
            return documentEditor.GetChangedDocument();
        }
    }
}
