// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
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
        internal static IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var service = document.GetLanguageService<ISyntaxFormattingService>();
            if (service != null)
            {
                return service.GetDefaultFormattingRules();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<AbstractFormattingRule>();
            }
        }

        /// <summary>
        /// Gets the formatting rules that would be applied if left unspecified.
        /// </summary>
        internal static IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules(Workspace workspace, string language)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            var service = workspace.Services.GetLanguageServices(language).GetService<ISyntaxFormattingService>();
            if (service != null)
            {
                return service.GetDefaultFormattingRules();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<AbstractFormattingRule>();
            }
        }

        /// <summary>
        /// Formats the whitespace in a document.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted document.</returns>
        public static Task<Document> FormatAsync(Document document, OptionSet options = null, CancellationToken cancellationToken = default)
            => FormatAsync(document, spans: null, options: options, cancellationToken: cancellationToken);

        /// <summary>
        /// Formats the whitespace in an area of a document corresponding to a text span.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="span">The span of the document's text to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted document.</returns>
        public static Task<Document> FormatAsync(Document document, TextSpan span, OptionSet options = null, CancellationToken cancellationToken = default)
            => FormatAsync(document, SpecializedCollections.SingletonEnumerable(span), options, cancellationToken);

        /// <summary>
        /// Formats the whitespace in areas of a document corresponding to multiple non-overlapping spans.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="spans">The spans of the document's text to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted document.</returns>
        public static Task<Document> FormatAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var formattingService = document.GetLanguageService<IFormattingService>();
            return formattingService == null
                ? SpecializedTasks.FromResult(document)
                : formattingService.FormatAsync(document, spans, options, cancellationToken);
        }

        internal static async Task<Document> FormatAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = options ?? await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(Format(root, spans, document.Project.Solution.Workspace, documentOptions, rules, cancellationToken));
        }

        /// <summary>
        /// Formats the whitespace in areas of a document corresponding to annotated nodes.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="annotation">The annotation used to find on nodes to identify spans to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted document.</returns>
        public static Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, OptionSet options = null, CancellationToken cancellationToken = default)
            => FormatAsync(document, annotation, options, rules: null, cancellationToken: cancellationToken);

        internal static async Task<Document> FormatAsync(Document document, SyntaxAnnotation annotation, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = options ?? await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(Format(root, annotation, document.Project.Solution.Workspace, documentOptions, rules, cancellationToken));
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
        public static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => Format(node, annotation, workspace, options, rules: null, cancellationToken: cancellationToken);

        internal static SyntaxNode Format(SyntaxNode node, SyntaxAnnotation annotation, Workspace workspace, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
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

            var spans = (annotation == SyntaxAnnotation.ElasticAnnotation)
                ? GetElasticSpans(node)
                : GetAnnotatedSpans(node, annotation);

            return Format(node, spans, workspace, options, rules, cancellationToken);
        }

        /// <summary>
        /// Formats the whitespace of a syntax tree.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="workspace">A workspace used to give the formatting context.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted tree's root node.</returns>
        public static SyntaxNode Format(SyntaxNode node, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => Format(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), workspace, options, rules: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Formats the whitespace in areas of a syntax tree identified by a span.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="span">The span within the node's full span to format.</param>
        /// <param name="workspace">A workspace used to give the formatting context.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted tree's root node.</returns>
        public static SyntaxNode Format(SyntaxNode node, TextSpan span, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => Format(node, SpecializedCollections.SingletonEnumerable(span), workspace, options, rules: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Formats the whitespace in areas of a syntax tree identified by multiple non-overlapping spans.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="spans">The spans within the node's full span to format.</param>
        /// <param name="workspace">A workspace used to give the formatting context.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted tree's root node.</returns>
        public static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan> spans, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => Format(node, spans, workspace, options, rules: null, cancellationToken: cancellationToken);

        internal static SyntaxNode Format(SyntaxNode node, IEnumerable<TextSpan> spans, Workspace workspace, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            var formattingResult = GetFormattingResult(node, spans, workspace, options, rules, cancellationToken);
            return formattingResult == null ? node : formattingResult.GetFormattedRoot(cancellationToken);
        }

        internal static IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan> spans, Workspace workspace, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var languageFormatter = workspace.Services.GetLanguageServices(node.Language).GetService<ISyntaxFormattingService>();
            if (languageFormatter == null)
            {
                return null;
            }

            options ??= workspace.Options;
            rules ??= GetDefaultFormattingRules(workspace, node.Language);
            spans ??= SpecializedCollections.SingletonEnumerable(node.FullSpan);
            return languageFormatter.Format(node, spans, options, rules, cancellationToken);
        }

        /// <summary>
        /// Determines the changes necessary to format the whitespace of a syntax tree.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="workspace">A workspace used to give the formatting context.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The changes necessary to format the tree.</returns>
        public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(node.FullSpan), workspace, options, rules: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Determines the changes necessary to format the whitespace of a syntax tree.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="span">The span within the node's full span to format.</param>
        /// <param name="workspace">A workspace used to give the formatting context.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The changes necessary to format the tree.</returns>
        public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, TextSpan span, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => GetFormattedTextChanges(node, SpecializedCollections.SingletonEnumerable(span), workspace, options, rules: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Determines the changes necessary to format the whitespace of a syntax tree.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="spans">The spans within the node's full span to format.</param>
        /// <param name="workspace">A workspace used to give the formatting context.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The changes necessary to format the tree.</returns>
        public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan> spans, Workspace workspace, OptionSet options = null, CancellationToken cancellationToken = default)
            => GetFormattedTextChanges(node, spans, workspace, options, rules: null, cancellationToken: cancellationToken);

        internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, IEnumerable<TextSpan> spans, Workspace workspace, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            var formattingResult = GetFormattingResult(node, spans, workspace, options, rules, cancellationToken);
            return formattingResult == null
                ? SpecializedCollections.EmptyList<TextChange>()
                : formattingResult.GetTextChanges(cancellationToken);
        }

        private static IEnumerable<TextSpan> GetAnnotatedSpans(SyntaxNode node, SyntaxAnnotation annotation)
        {
            foreach (var nodeOrToken in node.GetAnnotatedNodesAndTokens(annotation))
            {
                var firstToken = nodeOrToken.IsNode ? nodeOrToken.AsNode().GetFirstToken(includeZeroWidth: true) : nodeOrToken.AsToken();
                var lastToken = nodeOrToken.IsNode ? nodeOrToken.AsNode().GetLastToken(includeZeroWidth: true) : nodeOrToken.AsToken();
                yield return GetSpan(firstToken, lastToken);
            }
        }

        private static TextSpan GetSpan(SyntaxToken firstToken, SyntaxToken lastToken)
        {
            var previousToken = firstToken.GetPreviousToken();
            var nextToken = lastToken.GetNextToken();

            if (previousToken.RawKind != 0)
            {
                firstToken = previousToken;
            }

            if (nextToken.RawKind != 0)
            {
                lastToken = nextToken;
            }

            return TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);
        }

        private static IEnumerable<TextSpan> GetElasticSpans(SyntaxNode root)
        {
            var tokens = root.GetAnnotatedTrivia(SyntaxAnnotation.ElasticAnnotation).Select(tr => tr.Token).Distinct();
            return AggregateSpans(tokens.Select(t => GetElasticSpan(t)));
        }

        private static TextSpan GetElasticSpan(SyntaxToken token)
        {
            return GetSpan(token, token);
        }

        private static IEnumerable<TextSpan> AggregateSpans(IEnumerable<TextSpan> spans)
        {
            var aggregateSpans = new List<TextSpan>();

            var last = default(TextSpan);
            foreach (var span in spans)
            {
                if (last == default)
                {
                    last = span;
                }
                else if (span.IntersectsWith(last))
                {
                    last = TextSpan.FromBounds(last.Start, span.End);
                }
                else
                {
                    aggregateSpans.Add(last);
                    last = span;
                }
            }

            if (last != default)
            {
                aggregateSpans.Add(last);
            }

            return aggregateSpans;
        }

        public static Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken)
            => document.GetLanguageService<IOrganizeImportsService>().OrganizeImportsAsync(document, cancellationToken);
    }
}
