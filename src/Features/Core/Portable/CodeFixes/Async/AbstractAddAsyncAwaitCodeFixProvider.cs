// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

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

        protected static bool TryGetExpressionType(
            SyntaxNode expression,
            SemanticModel semanticModel,
            out INamedTypeSymbol returnType)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression);
            returnType = typeInfo.Type as INamedTypeSymbol;
            return returnType != null;
        }

        protected static bool TryGetTaskType(SemanticModel semanticModel, out INamedTypeSymbol taskType)
        {
            var compilation = semanticModel.Compilation;
            taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            return taskType != null;
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
