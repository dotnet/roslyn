// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.Async
{
    internal abstract partial class AbstractAsyncCodeFix : CodeFixProvider
    {
        protected abstract Task<CodeAction> GetCodeFix(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            SyntaxNode node;
            if (!TryGetNode(root, context.Span, out node))
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();

            var codeAction = await GetCodeFix(root, node, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

            if (codeAction != null)
            {
                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private bool TryGetNode(SyntaxNode root, TextSpan span, out SyntaxNode node)
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
