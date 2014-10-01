// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers
{
    public abstract class CodeFixProviderBase : CodeFixProvider
    {
        protected abstract string GetCodeFixDescription(string ruleId);
        
        internal abstract Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken);

        public sealed override async Task<IEnumerable<CodeAction>> GetFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var actions = SpecializedCollections.EmptyEnumerable<CodeAction>();
            foreach (var diagnostic in context.Diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nodeToFix = root.FindNode(diagnostic.Location.SourceSpan);

                var newDocument = await GetUpdatedDocumentAsync(document, model, root, nodeToFix, diagnostic.Id, cancellationToken).ConfigureAwait(false);

                Debug.Assert(newDocument != null);
                if (newDocument != document)
                {
                    var codeFixDescription = GetCodeFixDescription(diagnostic.Id);
                    actions = actions.Concat(new MyCodeAction(codeFixDescription, newDocument));
                }
            }

            return actions;
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Document newDocument) :
                base(title, c => Task.FromResult(newDocument))
            {
            }
        }
    }
}
