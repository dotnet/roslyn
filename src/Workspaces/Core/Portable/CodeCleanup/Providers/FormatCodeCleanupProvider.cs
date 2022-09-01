// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    internal sealed class FormatCodeCleanupProvider : ICodeCleanupProvider
    {
        private readonly IEnumerable<AbstractFormattingRule>? _rules;

        public FormatCodeCleanupProvider(IEnumerable<AbstractFormattingRule>? rules = null)
        {
            _rules = rules;
        }

        public string Name => PredefinedCodeCleanupProviderNames.Format;

        public async Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        {
            var formatter = document.GetRequiredLanguageService<ISyntaxFormattingService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var result = formatter.GetFormattingResult(root, spans, options, _rules, cancellationToken);

            // apply changes to an old text if it already exists
            return document.TryGetText(out var oldText) ?
                document.WithText(oldText.WithChanges(result.GetTextChanges(cancellationToken))) :
                document.WithSyntaxRoot(result.GetFormattedRoot(cancellationToken));
        }

        public Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, SyntaxFormattingOptions options, HostWorkspaceServices services, CancellationToken cancellationToken)
        {
            var formatter = services.GetRequiredLanguageService<ISyntaxFormattingService>(root.Language);
            var result = formatter.GetFormattingResult(root, spans, options, _rules, cancellationToken);

            // apply changes to an old text if it already exists
            return (root.SyntaxTree != null && root.SyntaxTree.TryGetText(out var oldText)) ?
                root.SyntaxTree.WithChangedText(oldText.WithChanges(result.GetTextChanges(cancellationToken))).GetRootAsync(cancellationToken) :
                Task.FromResult(result.GetFormattedRoot(cancellationToken));
        }
    }
}
