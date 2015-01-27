// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class LocalSuppressMessageCodeAction : CodeAction
        {
            private readonly AbstractSuppressionCodeFixProvider fixer;
            private readonly string title;
            private readonly ISymbol targetSymbol;
            private readonly SyntaxNode targetNode;
            private readonly Document document;
            private readonly Diagnostic diagnostic;

            public LocalSuppressMessageCodeAction(AbstractSuppressionCodeFixProvider fixer, ISymbol targetSymbol, SyntaxNode targetNode, Document document, Diagnostic diagnostic)
            {
                this.fixer = fixer;
                this.targetSymbol = targetSymbol;
                this.targetNode = targetNode;
                this.document = document;
                this.diagnostic = diagnostic;

                this.title = FeaturesResources.SuppressWithLocalSuppressMessage;
            }

            protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newTargetNode = fixer.AddLocalSuppressMessageAttribute(targetNode, targetSymbol, diagnostic);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(targetNode, newTargetNode);
                return document.WithSyntaxRoot(newRoot);
            }

            public override string Title
            {
                get
                {
                    return this.title;
                }
            }

            internal SyntaxNode TargetNode_TestOnly
            {
                get
                {
                    return this.targetNode;
                }
            }
        }
    }
}
