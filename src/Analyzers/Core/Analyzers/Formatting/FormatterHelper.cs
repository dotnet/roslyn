// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Formatting.FormattingExtensions;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Formats whitespace in documents or syntax trees.
/// </summary>
internal static class FormatterHelper
{
    /// <summary>
    /// Gets the formatting rules that would be applied if left unspecified.
    /// </summary>
    internal static IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules(ISyntaxFormatting syntaxFormattingService)
        => syntaxFormattingService.GetDefaultFormattingRules();

    /// <summary>
    /// Formats the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted tree's root node.</returns>
    public static SyntaxNode Format(SyntaxNode node, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => Format(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), syntaxFormattingService, options, rules: null, cancellationToken: cancellationToken);

    public static SyntaxNode Format(SyntaxNode node, TextSpan spanToFormat, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => Format(node, SpecializedCollections.SingletonEnumerable(spanToFormat), syntaxFormattingService, options, rules: null, cancellationToken: cancellationToken);

    /// <summary>
    /// Formats the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree.</param>
    /// <param name="annotation">The descendant nodes of the root to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted tree's root node.</returns>
    public static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        => Format(node, GetAnnotatedSpans(node, annotation), syntaxFormattingService, options, rules, cancellationToken: cancellationToken);

    internal static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan> spans, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        => GetFormattingResult(node, spans, syntaxFormattingService, options, rules, cancellationToken).GetFormattedRoot(cancellationToken);

    internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan> spans, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        => GetFormattingResult(node, spans, syntaxFormattingService, options, rules, cancellationToken).GetTextChanges(cancellationToken);

    internal static IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan> spans, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        => syntaxFormattingService.GetFormattingResult(node, spans, options, rules, cancellationToken);

    /// <summary>
    /// Determines the changes necessary to format the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The changes necessary to format the tree.</returns>
    public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, ISyntaxFormatting syntaxFormattingService, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), syntaxFormattingService, options, rules: null, cancellationToken: cancellationToken);
}
