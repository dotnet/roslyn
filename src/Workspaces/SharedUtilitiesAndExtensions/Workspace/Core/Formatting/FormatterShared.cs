// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.Formatting.FormattingExtensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class FormatterShared
{
    extension(ISyntaxFormatting syntaxFormatting)
    {
        public Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, SyntaxFormattingOptions options, CancellationToken cancellationToken)
            => syntaxFormatting.FormatAsync(document, annotation, options, rules: default, cancellationToken);

        public async Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(syntaxFormatting.Format(root, annotation, options, rules, cancellationToken));
        }

        public SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
            => syntaxFormatting.Format(node, GetAnnotatedSpans(node, annotation), options, rules, cancellationToken);

        public SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
            => syntaxFormatting.GetFormattingResult(node, spans, options, rules, cancellationToken).GetFormattedRoot(cancellationToken);

        public IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
            => syntaxFormatting.GetFormattingResult(node, spans, options, rules, cancellationToken);
    }
}
