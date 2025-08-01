// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingExtensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class FormatterShared
{
    public static async Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;
        return document.WithSyntaxRoot(Format(root, annotation, services, options, rules, cancellationToken));
    }

    public static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, SolutionServices services, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        => Format(node, GetAnnotatedSpans(node, annotation), services, options, rules, cancellationToken);

    public static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan>? spans, SolutionServices services, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        => GetFormattingResult(node, spans, services, options, rules, cancellationToken).GetFormattedRoot(cancellationToken);

    public static IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SolutionServices services, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
    {
        var formatter = services.GetRequiredLanguageService<ISyntaxFormattingService>(node.Language);
        return formatter.GetFormattingResult(node, spans, options, rules, cancellationToken);
    }
}
