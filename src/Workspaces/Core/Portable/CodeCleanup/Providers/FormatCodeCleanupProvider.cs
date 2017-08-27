// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
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
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var textChanges = await Formatter.GetFormattedTextChangesAsync(root, spans, document.Project.Solution.Workspace, cancellationToken: cancellationToken).ConfigureAwait(false);
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
                var changes = await Formatter.GetFormattedTextChangesAsync(root, spans, workspace, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (changes.Count == 0)
                {
                    return root;
                }

                return await root.SyntaxTree.WithChangedText(oldText.WithChanges(changes)).GetRootAsync(cancellationToken).ConfigureAwait(false);
            }

            return await Formatter.FormatAsync(root, spans, workspace, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
