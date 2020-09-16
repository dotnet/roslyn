// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public abstract override FixAllProvider GetFixAllProvider();

        protected abstract Task<CodeAction> GetCodeActionAsync(
            SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (!TryGetNode(root, context.Span, out var node))
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();

            var codeAction = await GetCodeActionAsync(
                root, node, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);
            if (codeAction != null)
            {
                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private static bool TryGetNode(SyntaxNode root, TextSpan span, out SyntaxNode node)
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
