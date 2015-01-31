// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.CodeFixes.Async
{
    internal abstract partial class AbstractAddAsyncAwaitCodeFixProvider : AbstractAsyncCodeFix
    {
        protected abstract string GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract Task<SyntaxNode> GetNewRoot(SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken);

        protected override async Task<CodeAction> GetCodeFix(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = await this.GetNewRoot(root, node, semanticModel, diagnostic, document, cancellationToken).ConfigureAwait(false);
            if (newRoot != null)
            {
                return new MyCodeAction(
                    this.GetDescription(diagnostic, node, semanticModel, cancellationToken),
                    token => Task.FromResult(document.WithSyntaxRoot(newRoot)));
            }

            return null;
        }

        protected bool TryGetTypes(
            SyntaxNode expression,
            SemanticModel semanticModel,
            out INamedTypeSymbol source,
            out INamedTypeSymbol destination)
        {
            source = null;
            destination = null;

            var info = semanticModel.GetSymbolInfo(expression);
            var methodSymbol = info.Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            var compilation = semanticModel.Compilation;
            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            if (taskType == null)
            {
                return false;
            }

            var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
            if (returnType == null)
            {
                return false;
            }

            source = taskType;
            destination = returnType;
            return true;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
