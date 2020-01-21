// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Formats whitespace in documents or syntax trees.
    /// </summary>
    internal static class Formatter
    {
        /// <summary>
        /// Gets the formatting rules that would be applied if left unspecified.
        /// </summary>
        internal static IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules(ISyntaxFormattingService syntaxFormattingService)
        {
            if (syntaxFormattingService != null)
            {
                return syntaxFormattingService.GetDefaultFormattingRules();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<AbstractFormattingRule>();
            }
        }

        /// <summary>
        /// Formats the whitespace in an area of a document corresponding to a text span.
        /// </summary>
        /// <param name="syntaxTree">The document to format.</param>
        /// <param name="span">The span of the document's text to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted document.</returns>
        public static Task<SyntaxTree> FormatAsync(SyntaxTree syntaxTree, ISyntaxFormattingService syntaxFormattingService, TextSpan span, OptionSet options, CancellationToken cancellationToken)
            => FormatAsync(syntaxTree, syntaxFormattingService, SpecializedCollections.SingletonEnumerable(span), options, cancellationToken);

        /// <summary>
        /// Formats the whitespace in areas of a document corresponding to multiple non-overlapping spans.
        /// </summary>
        /// <param name="syntaxTree">The document to format.</param>
        /// <param name="spans">The spans of the document's text to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the document's workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted document.</returns>
        public static Task<SyntaxTree> FormatAsync(SyntaxTree syntaxTree, ISyntaxFormattingService syntaxFormattingService, IEnumerable<TextSpan> spans, OptionSet options, CancellationToken cancellationToken)
        {
            return FormatAsync(syntaxTree, syntaxFormattingService, spans, options, rules: null, cancellationToken);
        }

        internal static async Task<SyntaxTree> FormatAsync(SyntaxTree syntaxTree, ISyntaxFormattingService syntaxFormattingService, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = options ?? CompilerAnalyzerConfigOptions.Empty;
            return syntaxTree.WithRootAndOptions(Format(root, syntaxFormattingService, spans, documentOptions, rules, cancellationToken), syntaxTree.Options);
        }

        /// <summary>
        /// Formats the whitespace of a syntax tree.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The formatted tree's root node.</returns>
        public static SyntaxNode Format(SyntaxNode node, ISyntaxFormattingService syntaxFormattingService, OptionSet options, CancellationToken cancellationToken)
            => Format(node, syntaxFormattingService, SpecializedCollections.SingletonEnumerable(node.FullSpan), options, rules: null, cancellationToken: cancellationToken);

        internal static SyntaxNode Format(SyntaxNode node, ISyntaxFormattingService syntaxFormattingService, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            var formattingResult = GetFormattingResult(node, syntaxFormattingService, spans, options, rules, cancellationToken);
            return formattingResult == null ? node : formattingResult.GetFormattedRoot(cancellationToken);
        }

        internal static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, ISyntaxFormattingService syntaxFormattingService, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            var formattingResult = GetFormattingResult(node, syntaxFormattingService, spans, options, rules, cancellationToken);
            return formattingResult == null
                ? SpecializedCollections.EmptyList<TextChange>()
                : formattingResult.GetTextChanges(cancellationToken);
        }

        internal static IFormattingResult GetFormattingResult(SyntaxNode node, ISyntaxFormattingService syntaxFormattingService, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (syntaxFormattingService is null)
            {
                return null;
            }

            options ??= CompilerAnalyzerConfigOptions.Empty;
            rules ??= GetDefaultFormattingRules(syntaxFormattingService);
            spans ??= SpecializedCollections.SingletonEnumerable(node.FullSpan);
            return syntaxFormattingService.Format(node, spans, options, rules, cancellationToken);
        }

        /// <summary>
        /// Determines the changes necessary to format the whitespace of a syntax tree.
        /// </summary>
        /// <param name="node">The root node of a syntax tree to format.</param>
        /// <param name="options">An optional set of formatting options. If these options are not supplied the current set of options from the workspace will be used.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The changes necessary to format the tree.</returns>
        public static IList<TextChange> GetFormattedTextChanges(SyntaxNode node, ISyntaxFormattingService syntaxFormattingService, OptionSet options, CancellationToken cancellationToken)
            => GetFormattedTextChanges(node, syntaxFormattingService, SpecializedCollections.SingletonEnumerable(node.FullSpan), options, rules: null, cancellationToken: cancellationToken);
    }
}
