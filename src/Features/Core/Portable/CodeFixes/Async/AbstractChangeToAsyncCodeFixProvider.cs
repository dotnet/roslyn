// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.CodeFixes.Async
{
    internal abstract partial class AbstractChangeToAsyncCodeFixProvider : AbstractAsyncCodeFix
    {
        protected abstract Task<string> GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract Task<Tuple<SyntaxTree, SyntaxNode>> GetRootInOtherSyntaxTree(SyntaxNode node, SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken);

        protected override async Task<CodeAction> GetCodeFix(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var result = await GetRootInOtherSyntaxTree(node, semanticModel, diagnostic, cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                var syntaxTree = result.Item1;
                var newRoot = result.Item2;
                var otherDocument = document.Project.Solution.GetDocument(syntaxTree);
                return new MyCodeAction(
                    await this.GetDescription(diagnostic, node, semanticModel, cancellationToken).ConfigureAwait(false),
                    token => Task.FromResult(otherDocument.WithSyntaxRoot(newRoot)));
            }

            return null;
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
