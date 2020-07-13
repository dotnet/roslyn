// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract class AbstractInlineMethodRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context);
        protected abstract bool IsMethodContainsOneStatement(SyntaxNode methodDeclarationSyntaxNode);
        protected abstract SyntaxNode GetInlineContent(SyntaxNode methodDeclarationSyntax);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var methodInvocationNode = await GetInvocationExpressionSyntaxNodeAsync(context).ConfigureAwait(false);
            if (methodInvocationNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodDeclarationSymbol = semanticModel?.GetSymbolInfo(methodInvocationNode).Symbol;

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
                    $"Inline {methodSymbol.ToNameDisplayString()}",
                    cancellationToken => document.ReplaceNodeAsync(methodInvocationNode, GetInlineContent(methodDeclarationSyntaxNode), cancellationToken));

                context.RegisterRefactoring(codeAction);
            }
        }
    }
}
