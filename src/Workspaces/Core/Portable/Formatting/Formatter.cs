// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Formatting.FormattingExtensions;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Formats whitespace in documents or syntax trees.
/// </summary>
public static class Formatter
{
    /// <summary>
    /// The annotation used to mark portions of a syntax tree to be formatted.
    /// </summary>
    public static SyntaxAnnotation Annotation { get; } = new SyntaxAnnotation();

    /// <summary>
    /// Gets the formatting rules that would be applied if left unspecified.
    /// </summary>
    internal static ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules(Document document)
        => GetDefaultFormattingRules(document.Project.Services);

    internal static ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules(LanguageServices languageServices)
        => languageServices.GetService<ISyntaxFormattingService>()?.GetDefaultFormattingRules() ?? [];

    /// <summary>
    /// Formats the whitespace in a document.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted document.</returns>
    public static Task<Document> FormatAsync(Document document, OptionSet? options = null, CancellationToken cancellationToken = default)
#pragma warning disable RS0030 // Do not used banned APIs
        => FormatAsync(document, spans: null, options: options, cancellationToken: cancellationToken);
#pragma warning restore

    internal static Task<Document> FormatAsync(Document document, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => FormatAsync(document, spans: null, options, rules: null, cancellationToken);

    /// <summary>
    /// Formats the whitespace in an area of a document corresponding to a text span.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="span">The span of the document's text to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted document.</returns>
    public static Task<Document> FormatAsync(Document document, TextSpan span, OptionSet? options = null, CancellationToken cancellationToken = default)
#pragma warning disable RS0030 // Do not used banned APIs
        => FormatAsync(document, SpecializedCollections.SingletonEnumerable(span), options, cancellationToken);
#pragma warning restore

    internal static Task<Document> FormatAsync(Document document, TextSpan span, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => FormatAsync(document, SpecializedCollections.SingletonEnumerable(span), options, rules: null, cancellationToken);

    /// <summary>
    /// Formats the whitespace in areas of a document corresponding to multiple non-overlapping spans.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="spans">The spans of the document's text to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted document.</returns>
    public static async Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, OptionSet? options = null, CancellationToken cancellationToken = default)
    {
        var formattingService = document.GetLanguageService<IFormattingService>();
        if (formattingService == null)
        {
            return document;
        }

        var (syntaxFormattingOptions, lineFormattingOptions) = await GetFormattingOptionsAsync(document, options, cancellationToken).ConfigureAwait(false);
        return await formattingService.FormatAsync(document, spans, lineFormattingOptions, syntaxFormattingOptions, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;
        return document.WithSyntaxRoot(Format(root, spans, services, options, rules, cancellationToken));
    }

    /// <summary>
    /// Formats the whitespace in areas of a document corresponding to annotated nodes.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="annotation">The annotation used to find on nodes to identify spans to format.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted document.</returns>
    public static Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, OptionSet? options = null, CancellationToken cancellationToken = default)
        => FormatAsync(document, annotation, options, rules: null, cancellationToken: cancellationToken);

    internal static Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => FormatAsync(document, annotation, options, rules: null, cancellationToken);

    internal static async Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;
        return document.WithSyntaxRoot(Format(root, annotation, services, options, rules, cancellationToken));
    }

    internal static async Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, OptionSet? optionSet, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (annotation == null)
        {
            throw new ArgumentNullException(nameof(annotation));
        }

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;

        // must have syntax formatting options since we require the document to have a syntax tree:
        var (formattingOptions, _) = await GetFormattingOptionsAsync(document, optionSet, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(formattingOptions);

        return document.WithSyntaxRoot(Format(root, annotation, services, formattingOptions, rules, cancellationToken));
    }

    /// <summary>
    /// Formats the whitespace in areas of a syntax tree corresponding to annotated nodes.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="annotation">The annotation used to find nodes to identify spans to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted tree's root node.</returns>
    public static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => Format(node, annotation, workspace, options, rules: null, cancellationToken);

    internal static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, SolutionServices services, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => Format(node, annotation, services, options, rules: null, cancellationToken);

    private static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, Workspace workspace, OptionSet? options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (annotation == null)
        {
            throw new ArgumentNullException(nameof(annotation));
        }

        return Format(node, GetAnnotatedSpans(node, annotation), workspace, options, rules, cancellationToken);
    }

    internal static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, SolutionServices services, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        => Format(node, GetAnnotatedSpans(node, annotation), services, options, rules, cancellationToken);

    /// <summary>
    /// Formats the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted tree's root node.</returns>
    public static SyntaxNode Format(SyntaxNode node, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => Format(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), workspace, options, rules: null, cancellationToken);

    internal static SyntaxNode Format(SyntaxNode node, SolutionServices services, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => Format(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), services, options, rules: null, cancellationToken);

    /// <summary>
    /// Formats the whitespace in areas of a syntax tree identified by a span.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="span">The span within the node's full span to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted tree's root node.</returns>
    public static SyntaxNode Format(SyntaxNode node, TextSpan span, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => Format(node, SpecializedCollections.SingletonEnumerable(span), workspace, options, rules: null, cancellationToken: cancellationToken);

    internal static SyntaxNode Format(SyntaxNode node, TextSpan span, SolutionServices services, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => Format(node, SpecializedCollections.SingletonEnumerable(span), services, options, rules: null, cancellationToken: cancellationToken);

    /// <summary>
    /// Formats the whitespace in areas of a syntax tree identified by multiple non-overlapping spans.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="spans">The spans within the node's full span to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The formatted tree's root node.</returns>
    public static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan>? spans, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => Format(node, spans, workspace, options, rules: null, cancellationToken: cancellationToken);

    private static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan>? spans, Workspace workspace, OptionSet? options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        var formattingResult = GetFormattingResult(node, spans, workspace, options, rules, cancellationToken);
        return formattingResult == null ? node : formattingResult.GetFormattedRoot(cancellationToken);
    }

    internal static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan>? spans, SolutionServices services, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        => GetFormattingResult(node, spans, services, options, rules, cancellationToken).GetFormattedRoot(cancellationToken);

    private static IFormattingResult? GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, Workspace workspace, OptionSet? options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var languageServices = workspace.Services.GetLanguageServices(node.Language);
        var languageFormatter = languageServices.GetService<ISyntaxFormattingService>();
        if (languageFormatter == null)
        {
            return null;
        }

        spans ??= SpecializedCollections.SingletonEnumerable(node.FullSpan);
        var formattingOptions = GetFormattingOptions(workspace, options, node.Language);
        return languageFormatter.GetFormattingResult(node, spans, formattingOptions, rules, cancellationToken);
    }

    internal static IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SolutionServices services, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        var formatter = services.GetRequiredLanguageService<ISyntaxFormattingService>(node.Language);
        return formatter.GetFormattingResult(node, spans, options, rules, cancellationToken);
    }

    /// <summary>
    /// Determines the changes necessary to format the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The changes necessary to format the tree.</returns>
    public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), workspace, options, rules: null, cancellationToken: cancellationToken);

    internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, SolutionServices services, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), services, options, rules: null, cancellationToken: cancellationToken);

    /// <summary>
    /// Determines the changes necessary to format the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="span">The span within the node's full span to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The changes necessary to format the tree.</returns>
    public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, TextSpan span, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(span), workspace, options, rules: null, cancellationToken);

    internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, TextSpan span, SolutionServices services, SyntaxFormattingOptions options, CancellationToken cancellationToken = default)
        => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(span), services, options, rules: null, cancellationToken);

    /// <summary>
    /// Determines the changes necessary to format the whitespace of a syntax tree.
    /// </summary>
    /// <param name="node">The root node of a syntax tree to format.</param>
    /// <param name="spans">The spans within the node's full span to format.</param>
    /// <param name="workspace">A workspace used to give the formatting context.</param>
    /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The changes necessary to format the tree.</returns>
    public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan>? spans, Workspace workspace, OptionSet? options = null, CancellationToken cancellationToken = default)
        => GetFormattedTextChanges(node, spans, workspace, options, rules: null, cancellationToken);

    internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan>? spans, SolutionServices services, SyntaxFormattingOptions options, CancellationToken cancellationToken = default)
        => GetFormattedTextChanges(node, spans, services, options, rules: null, cancellationToken);

    private static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan>? spans, Workspace workspace, OptionSet? options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
    {
        var formattingResult = GetFormattingResult(node, spans, workspace, options, rules, cancellationToken);
        return formattingResult == null
            ? SpecializedCollections.EmptyList<TextChange>()
            : formattingResult.GetTextChanges(cancellationToken);
    }

    internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan>? spans, SolutionServices services, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken = default)
    {
        var formatter = services.GetRequiredLanguageService<ISyntaxFormattingService>(node.Language);
        return formatter.GetFormattingResult(node, spans, options, rules, cancellationToken).GetTextChanges(cancellationToken);
    }

    internal static SyntaxFormattingOptions GetFormattingOptions(Workspace workspace, OptionSet? optionSet, string language)
    {
        var syntaxFormattingService = workspace.Services.GetRequiredLanguageService<ISyntaxFormattingService>(language);
        return syntaxFormattingService.GetFormattingOptions(optionSet ?? workspace.CurrentSolution.Options, fallbackOptions: null);
    }

#pragma warning disable RS0030 // Do not used banned APIs (backwards compatibility)
    internal static async ValueTask<(SyntaxFormattingOptions? Syntax, LineFormattingOptions Line)> GetFormattingOptionsAsync(Document document, OptionSet? optionSet, CancellationToken cancellationToken)
    {
        optionSet ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

        LineFormattingOptions lineFormattingOptions;
        SyntaxFormattingOptions? syntaxFormattingOptions;

        var syntaxFormattingService = document.GetLanguageService<ISyntaxFormattingService>();
        if (syntaxFormattingService != null)
        {
            syntaxFormattingOptions = syntaxFormattingService.GetFormattingOptions(optionSet, fallbackOptions: null);
            lineFormattingOptions = syntaxFormattingOptions.LineFormatting;
        }
        else
        {
            syntaxFormattingOptions = null;
            lineFormattingOptions = optionSet.GetLineFormattingOptions(document.Project.Language, fallbackOptions: null);
        }

        return (syntaxFormattingOptions, lineFormattingOptions);
    }
#pragma warning restore

    /// <summary>
    /// Organizes the imports in the document.
    /// </summary>
    /// <param name="document">The document to organize.</param>
    /// <param name="cancellationToken">The cancellation token that the operation will observe.</param>
    /// <returns>The document with organized imports. If the language does not support organizing imports, or if no changes were made, this method returns <paramref name="document"/>.</returns>
    public static async Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken = default)
    {
        var organizeImportsService = document.GetLanguageService<IOrganizeImportsService>();
        if (organizeImportsService is null)
        {
            return document;
        }

        var options = await GetOrganizeImportsOptionsAsync(document, cancellationToken).ConfigureAwait(false);
        return await organizeImportsService.OrganizeImportsAsync(document, options, cancellationToken).ConfigureAwait(false);
    }

#pragma warning disable RS0030 // Do not used banned APIs (backwards compatibility)
    internal static async ValueTask<OrganizeImportsOptions> GetOrganizeImportsOptionsAsync(Document document, CancellationToken cancellationToken)
    {
        var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        return optionSet.GetOrganizeImportsOptions(document.Project.Language, fallbackOptions: null);
    }
#pragma warning restore
}
