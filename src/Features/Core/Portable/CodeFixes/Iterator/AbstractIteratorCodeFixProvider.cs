// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.Iterator
{
    internal abstract class AbstractIteratorCodeFixProvider : CodeFixProvider
    {
        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported by this code fix
            return null;
        }

        protected abstract Task<CodeAction> GetCodeFixAsync(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostics, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (!TryGetNode(root, context.Span, out var node))
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();

            var codeAction = await GetCodeFixAsync(root, node, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

            if (codeAction != null)
            {
                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        protected virtual bool TryGetNode(SyntaxNode root, TextSpan span, out SyntaxNode node)
        {
            node = null;
            var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();
            if (!ancestors.Any())
            {
                return false;
            }

            node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
            return node != null;
        }
    }
}
