// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.GenerateMember
{
    internal abstract class AbstractGenerateMemberCodeFixProvider : CodeFixProvider
    {
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // TODO: https://github.com/dotnet/roslyn/issues/5777
            // Not supported in REPL for now.
            if (context.Project.IsSubmission)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var names = GetTargetNodes(root, context.Span, diagnostic);
            foreach (var name in names)
            {
                var codeActions = await GetCodeActionsAsync(context.Document, name, context.CancellationToken).ConfigureAwait(false);
                if (codeActions == null || codeActions.IsEmpty())
                {
                    continue;
                }

                context.RegisterFixes(codeActions, context.Diagnostics);
                return;
            }
        }

        protected abstract Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);

        protected virtual SyntaxNode GetTargetNode(SyntaxNode node)
        {
            return node;
        }

        protected virtual bool IsCandidate(SyntaxNode node, Diagnostic diagnostic)
        {
            return false;
        }

        protected virtual IEnumerable<SyntaxNode> GetTargetNodes(SyntaxNode root, TextSpan span, Diagnostic diagnostic)
        {
            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                yield break;
            }

            var nodes = token.GetAncestors<SyntaxNode>().Where(n => IsCandidate(n, diagnostic));
            foreach (var node in nodes)
            {
                var name = GetTargetNode(node);

                if (name != null)
                {
                    yield return name;
                }
            }
        }
    }
}
