// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    internal class FormatCodeCleanupProvider : ICodeCleanupProvider
    {
        public string Name => PredefinedCodeCleanupProviderNames.Format;

        public async Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
        {
            // If the old text already exists, use the fast path for formatting.
            if (document.TryGetText(out var oldText))
            {
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var textChanges = Formatter.GetFormattedTextChanges(root, spans, document.Project.Solution.Workspace, cancellationToken: cancellationToken);
                if (textChanges.Count == 0)
                {
                    return document;
                }

                var newText = oldText.WithChanges(textChanges);
                return document.WithText(newText);
            }

            return await Formatter.FormatAsync(document, spans, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            // If the old text already exists, use the fast path for formatting.
            if (root.SyntaxTree != null && root.SyntaxTree.TryGetText(out var oldText))
            {
                var changes = Formatter.GetFormattedTextChanges(root, spans, workspace, cancellationToken: cancellationToken);
                if (changes.Count == 0)
                {
                    return root;
                }

                return await root.SyntaxTree.WithChangedText(oldText.WithChanges(changes)).GetRootAsync(cancellationToken).ConfigureAwait(false);
            }

            return Formatter.Format(root, spans, workspace, cancellationToken: cancellationToken);
        }
    }
}
