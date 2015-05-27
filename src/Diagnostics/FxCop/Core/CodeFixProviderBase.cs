// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.AnalyzerPowerPack
{
    public abstract class CodeFixProviderBase : CodeFixProvider
    {
        protected abstract string GetCodeFixDescription(Diagnostic diagnostic);

        internal abstract Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nodeToFix = root.FindNode(diagnostic.Location.SourceSpan);

                var newDocument = await GetUpdatedDocumentAsync(document, model, root, nodeToFix, diagnostic, cancellationToken).ConfigureAwait(false);

                Debug.Assert(newDocument != null);
                if (newDocument != document)
                {
                    var codeFixDescription = GetCodeFixDescription(diagnostic);
                    context.RegisterCodeFix(new MyCodeAction(codeFixDescription, newDocument), diagnostic);
                }
            }
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Document newDocument) :
                base(title, c => Task.FromResult(newDocument))
            {
            }
        }
    }
}
