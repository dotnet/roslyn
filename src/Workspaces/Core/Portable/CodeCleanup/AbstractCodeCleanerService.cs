// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal abstract class AbstractCodeCleanerService : ICodeCleanerService
    {
        public abstract IEnumerable<ICodeCleanupProvider> GetDefaultProviders();

        public async Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_CleanupAsync, cancellationToken))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // If there is no span to format...
                if (!spans.Any())
                {
                    // ... then return the Document unchanged
                    return document;
                }

                var codeCleaners = providers ?? GetDefaultProviders();

                var normalizedSpan = spans.ToNormalizedSpans();
                if (CleanupWholeNode(root.FullSpan, normalizedSpan))
                {
                    // We are cleaning up the whole document, so there is no need to do expansive span tracking between cleaners.
                    return await IterateAllCodeCleanupProvidersAsync(document, document, r => SpecializedCollections.SingletonEnumerable(r.FullSpan), codeCleaners, cancellationToken).ConfigureAwait(false);
                }

                var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                Contract.Requires(syntaxFactsService != null);

                // We need to track spans between cleaners. Annotate the tree with the provided spans.
                var newNodeAndAnnotations = AnnotateNodeForTextSpans(syntaxFactsService, root, normalizedSpan, cancellationToken);

                // If it urns out we don't need to annotate anything since all spans are merged to one span that covers the whole node...
                if (newNodeAndAnnotations.Item1 == null)
                {
                    // ... then we are cleaning up the whole document, so there is no need to do expansive span tracking between cleaners.
                    return await IterateAllCodeCleanupProvidersAsync(document, document, n => SpecializedCollections.SingletonEnumerable(n.FullSpan), codeCleaners, cancellationToken).ConfigureAwait(false);
                }

                var model = await document.GetSemanticModelForSpanAsync(spans.Collapse(), cancellationToken).ConfigureAwait(false);

                // Replace the initial node and document with the annotated node.
                var annotatedRoot = newNodeAndAnnotations.Item1;
                var annotatedDocument = document.WithSyntaxRoot(annotatedRoot);

                // Run the actual cleanup.
                return await IterateAllCodeCleanupProvidersAsync(document, annotatedDocument, r => GetTextSpansFromAnnotation(r, newNodeAndAnnotations.Item2, cancellationToken), codeCleaners, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<SyntaxNode> CleanupAsync(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_Cleanup, cancellationToken))
            {
                // If there is no span to format...
                if (!spans.Any())
                {
                    // ... then return the Document unchanged
                    return root;
                }

                var codeCleaners = providers ?? GetDefaultProviders();

                var normalizedSpan = spans.ToNormalizedSpans();
                if (CleanupWholeNode(root.FullSpan, normalizedSpan))
                {
                    // We are cleaning up the whole document, so there is no need to do expansive span tracking between cleaners.
                    return await IterateAllCodeCleanupProvidersAsync(root, root, r => SpecializedCollections.SingletonEnumerable(r.FullSpan), workspace, codeCleaners, cancellationToken).ConfigureAwait(false);
                }

                var syntaxFactsService = workspace.Services.GetLanguageServices(root.Language).GetService<ISyntaxFactsService>();
                Contract.Requires(syntaxFactsService != null);

                // We need to track spans between cleaners. Annotate the tree with the provided spans.
                var newNodeAndAnnotations = AnnotateNodeForTextSpans(syntaxFactsService, root, normalizedSpan, cancellationToken);

                // If it urns out we don't need to annotate anything since all spans are merged to one span that covers the whole node...
                if (newNodeAndAnnotations.Item1 == null)
                {
                    // ... then we are cleaning up the whole document, so there is no need to do expansive span tracking between cleaners.
                    return await IterateAllCodeCleanupProvidersAsync(root, root, n => SpecializedCollections.SingletonEnumerable(n.FullSpan), workspace, codeCleaners, cancellationToken).ConfigureAwait(false);
                }

                // Replace the initial node and document with the annotated node.
                var annotatedRoot = newNodeAndAnnotations.Item1;

                // Run the actual cleanup.
                return await IterateAllCodeCleanupProvidersAsync(root, annotatedRoot, r => GetTextSpansFromAnnotation(r, newNodeAndAnnotations.Item2, cancellationToken), workspace, codeCleaners, cancellationToken).ConfigureAwait(false);
            }
        }

        private IEnumerable<TextSpan> GetTextSpansFromAnnotation(SyntaxNode node, List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
        {
            // Now try to retrieve the text span from the annotations injected into the node.
            foreach (var annotationPair in annotations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var previousMarkerAnnotation = annotationPair.Item1;
                var nextMarkerAnnotation = annotationPair.Item2;

                var previousTokenMarker = SpanMarker.FromAnnotation(previousMarkerAnnotation);
                var nextTokenMarker = SpanMarker.FromAnnotation(nextMarkerAnnotation);

                var previousTokens = node.GetAnnotatedNodesAndTokens(previousMarkerAnnotation).Where(n => n.IsToken).Select(n => n.AsToken());
                var nextTokens = node.GetAnnotatedNodesAndTokens(nextMarkerAnnotation).Where(n => n.IsToken).Select(n => n.AsToken());

                // Check whether we can use the tokens we got back from the node.
                TextSpan span;
                if (TryGetTextSpanFromAnnotation(previousTokenMarker, nextTokenMarker, node, previousTokens, nextTokens, out span))
                {
                    yield return span;
                }
            }
        }

        private bool TryGetTextSpanFromAnnotation(
            SpanMarker previousTokenMarker,
            SpanMarker nextTokenMarker,
            SyntaxNode node,
            IEnumerable<SyntaxToken> previousTokens,
            IEnumerable<SyntaxToken> nextTokens,
            out TextSpan span)
        {
            // Set initial value
            span = default(TextSpan);

            var previousToken = previousTokens.FirstOrDefault();
            var nextToken = nextTokens.FirstOrDefault();

            // Define all variables required to determine state
            var hasNoPreviousToken = previousToken.RawKind == 0;
            var hasNoNextToken = nextToken.RawKind == 0;

            var hasMultiplePreviousToken = previousTokens.Skip(1).Any();
            var hasMultipleNextToken = nextTokens.Skip(1).Any();

            var hasOnePreviousToken = !hasNoPreviousToken && !hasMultiplePreviousToken;
            var hasOneNextToken = !hasNoNextToken && !hasMultipleNextToken;

            // Normal case
            if (hasOnePreviousToken && hasOneNextToken)
            {
                return TryCreateTextSpan(GetPreviousTokenStartPosition(previousTokenMarker.Type, previousToken),
                                         GetNextTokenEndPosition(nextTokenMarker.Type, nextToken),
                                         out span);
            }

            // Quick error check * we don't have any token, ignore this one.
            // This can't happen unless one of cleaners violated the contract of only changing things inside of the provided span.
            if (hasNoPreviousToken && hasNoNextToken)
            {
                return false;
            }

            // We don't have the previous token, but we do have one next token.
            // nextTokenMarker has hint to how to find the opposite marker.
            // If we can't find the other side, then it means one of cleaners has violated the contract. Ignore span.
            if (hasNoPreviousToken && hasOneNextToken)
            {
                if (nextTokenMarker.OppositeMarkerType == SpanMarkerType.BeginningOfFile)
                {
                    // Okay, found right span
                    return TryCreateTextSpan(node.SpanStart, GetNextTokenEndPosition(nextTokenMarker.Type, nextToken), out span);
                }

                return false;
            }

            // One previous token but no next token with hint case
            if (hasOnePreviousToken && hasNoNextToken)
            {
                if (previousTokenMarker.OppositeMarkerType == SpanMarkerType.EndOfFile)
                {
                    // Okay, found right span
                    return TryCreateTextSpan(GetPreviousTokenStartPosition(previousTokenMarker.Type, previousToken), node.Span.End, out span);
                }

                return false;
            }

            // Now the simple cases are done. Now we need to deal with cases where annotations found more than one corresponding token.
            // Mostly it means one of cleaners violated the contract, so we can just ignore the span except in one cases where it involves the beginning and end of the tree.
            Contract.ThrowIfFalse(hasMultiplePreviousToken || hasMultipleNextToken);

            // Check whether it is one of special cases or not
            if (hasMultiplePreviousToken && previousTokenMarker.Type == SpanMarkerType.BeginningOfFile)
            {
                // Okay, it is an edge case. Let's use the start of the node as the beginning of the span
                span = TextSpan.FromBounds(node.SpanStart, GetNextTokenEndPosition(nextTokenMarker.Type, nextToken));
                return true;
            }

            if (hasMultipleNextToken && nextTokenMarker.Type == SpanMarkerType.EndOfFile)
            {
                // Okay, it is an edge case. Let's use the end of the node as the end of the span
                span = TextSpan.FromBounds(GetPreviousTokenStartPosition(previousTokenMarker.Type, previousToken), node.Span.End);
                return true;
            }

            // All other cases are invalid cases where one of code cleaners messed things up by moving around things it shouldn't move.
            return false;
        }

        /// <summary>
        /// Get the proper start position based on the span marker type.
        /// </summary>
        private int GetPreviousTokenStartPosition(SpanMarkerType spanMarkerType, SyntaxToken previousToken)
        {
            Contract.ThrowIfTrue(spanMarkerType == SpanMarkerType.EndOfFile);
            Contract.ThrowIfTrue(previousToken.RawKind == 0);

            if (spanMarkerType == SpanMarkerType.Normal)
            {
                return previousToken.GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true).SpanStart;
            }

            return previousToken.SpanStart;
        }

        /// <summary>
        /// Get the proper end position based on the span marker type.
        /// </summary>
        private int GetNextTokenEndPosition(SpanMarkerType spanMarkerType, SyntaxToken nextToken)
        {
            Contract.ThrowIfTrue(spanMarkerType == SpanMarkerType.BeginningOfFile);
            Contract.ThrowIfTrue(nextToken.RawKind == 0);

            if (spanMarkerType == SpanMarkerType.Normal)
            {
                return nextToken.GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true).Span.End;
            }

            return nextToken.Span.End;
        }

        /// <summary>
        /// Inject annotations into the node so that it can re-calculate spans for each code cleaner after each tree transformation.
        /// </summary>
        private ValueTuple<SyntaxNode, List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>>> AnnotateNodeForTextSpans(
            ISyntaxFactsService syntaxFactsService, SyntaxNode root, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            // Get spans where the tokens around the spans are not overlapping with the spans.
            var nonOverlappingSpans = GetNonOverlappingSpans(syntaxFactsService, root, spans, cancellationToken);

            // Build token annotation map
            var tokenAnnotationMap = new Dictionary<SyntaxToken, List<SyntaxAnnotation>>();
            var annotations = new List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>>();
            foreach (var span in nonOverlappingSpans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SyntaxToken previousToken;
                SyntaxToken startToken;
                SyntaxToken endToken;
                SyntaxToken nextToken;
                GetTokensAroundSpan(root, span, out previousToken, out startToken, out endToken, out nextToken);

                // Create marker to insert
                SpanMarker startMarker = new SpanMarker(type: (previousToken.RawKind == 0) ? SpanMarkerType.BeginningOfFile : SpanMarkerType.Normal,
                                                        oppositeMarkerType: (nextToken.RawKind == 0) ? SpanMarkerType.EndOfFile : SpanMarkerType.Normal);

                SpanMarker endMarker = new SpanMarker(type: (nextToken.RawKind == 0) ? SpanMarkerType.EndOfFile : SpanMarkerType.Normal,
                                                      oppositeMarkerType: (previousToken.RawKind == 0) ? SpanMarkerType.BeginningOfFile : SpanMarkerType.Normal);

                // Set proper tokens
                previousToken = (previousToken.RawKind == 0) ? root.GetFirstToken(includeZeroWidth: true) : previousToken;
                nextToken = (nextToken.RawKind == 0) ? root.GetLastToken(includeZeroWidth: true) : nextToken;

                // Build token to marker map
                tokenAnnotationMap.GetOrAdd(previousToken, _ => new List<SyntaxAnnotation>()).Add(startMarker.Annotation);
                tokenAnnotationMap.GetOrAdd(nextToken, _ => new List<SyntaxAnnotation>()).Add(endMarker.Annotation);

                // Remember markers
                annotations.Add(new ValueTuple<SyntaxAnnotation, SyntaxAnnotation>(startMarker.Annotation, endMarker.Annotation));
            }

            // Do a quick check.
            // If, after all merges, the spans are merged into one span that covers the whole tree, return right away.
            if (CleanupWholeNode(annotations))
            {
                // This will indicate that no annotation is needed.
                return default(ValueTuple<SyntaxNode, List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>>>);
            }

            // Inject annotations
            var newNode = InjectAnnotations(root, tokenAnnotationMap);
            return ValueTuple.Create(newNode, annotations);
        }

        /// <summary>
        /// Make sure annotations are positioned outside of any spans. If not, merge two adjacent spans to one.
        /// </summary>
        private IEnumerable<TextSpan> GetNonOverlappingSpans(ISyntaxFactsService syntaxFactsService, SyntaxNode root, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            // Create interval tree for spans
            var intervalTree = SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, spans);

            // Find tokens that are outside of spans
            var tokenSpans = new List<TextSpan>();
            foreach (var span in spans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SyntaxToken startToken;
                SyntaxToken endToken;
                TextSpan spanAlignedToTokens = GetSpanAlignedToTokens(syntaxFactsService, root, span, out startToken, out endToken);

                SyntaxToken previousToken = startToken.GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
                SyntaxToken nextToken = endToken.GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

                // Make sure the previous and next tokens we found do not overlap with any existing spans. If they do, merge two spans.
                previousToken = (previousToken.RawKind == 0) ? root.GetFirstToken(includeZeroWidth: true) : previousToken;
                var start = intervalTree.GetOverlappingIntervals(previousToken.SpanStart, previousToken.Span.Length).Any() ?
                    previousToken.SpanStart : startToken.SpanStart;

                nextToken = (nextToken.RawKind == 0) ? root.GetLastToken(includeZeroWidth: true) : nextToken;
                var end = intervalTree.GetOverlappingIntervals(nextToken.SpanStart, nextToken.Span.Length).Any() ?
                    nextToken.Span.End : endToken.Span.End;

                tokenSpans.Add(TextSpan.FromBounds(start, end));
            }

            return tokenSpans.ToNormalizedSpans();
        }

        /// <summary>
        /// Retrieves four tokens around span like below.
        ///
        /// [previousToken][startToken][SPAN][endToken][nextToken]
        /// </summary>
        private static void GetTokensAroundSpan(
            SyntaxNode root, TextSpan span,
            out SyntaxToken previousToken,
            out SyntaxToken startToken,
            out SyntaxToken endToken,
            out SyntaxToken nextToken)
        {
            // Get tokens at the edges of the span
            startToken = root.FindToken(span.Start, findInsideTrivia: true);

            endToken = root.FindTokenFromEnd(span.End, findInsideTrivia: true);

            // There must be tokens at each edge
            Contract.ThrowIfTrue(startToken.RawKind == 0 || endToken.RawKind == 0);

            // Get the previous and next tokens around span
            previousToken = startToken.GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            nextToken = endToken.GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
        }

        /// <summary>
        /// Adjust provided span to align to either token's start position or end position.
        /// </summary>
        private TextSpan GetSpanAlignedToTokens(
            ISyntaxFactsService syntaxFactsService, SyntaxNode root, TextSpan span, out SyntaxToken startToken, out SyntaxToken endToken)
        {
            startToken = FindTokenOnLeftOfPosition(syntaxFactsService, root, span.Start);
            endToken = FindTokenOnRightOfPosition(syntaxFactsService, root, span.End);

            // Found two different tokens
            if (startToken.Span.End <= endToken.SpanStart)
            {
                return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
            }
            else
            {
                // Found one token inside of the span
                endToken = startToken;
                return startToken.Span;
            }
        }

        /// <summary>
        /// Find closest token (including one in structured trivia) right of given position
        /// </summary>
        private SyntaxToken FindTokenOnRightOfPosition(ISyntaxFactsService syntaxFactsService, SyntaxNode root, int position)
        {
            var token = syntaxFactsService.FindTokenOnRightOfPosition(root, position, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            if (token.RawKind == 0)
            {
                return root.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            }

            return token;
        }

        /// <summary>
        /// Find closest token (including one in structured trivia) left of given position
        /// </summary>
        private SyntaxToken FindTokenOnLeftOfPosition(ISyntaxFactsService syntaxFactsService, SyntaxNode root, int position)
        {
            // find token on left
            var token = syntaxFactsService.FindTokenOnLeftOfPosition(root, position, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            if (token.RawKind == 0)
            {
                // if there is no token on left, return the first token
                return root.GetFirstToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            }

            return token;
        }

        private bool CleanupWholeNode(List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>> annotations)
        {
            if (annotations.Count != 1)
            {
                return false;
            }

            var startMarker = SpanMarker.FromAnnotation(annotations[0].Item1);
            var endMarker = SpanMarker.FromAnnotation(annotations[0].Item2);

            return startMarker.Type == SpanMarkerType.BeginningOfFile && endMarker.Type == SpanMarkerType.EndOfFile;
        }

        private bool CleanupWholeNode(TextSpan nodeSpan, IEnumerable<TextSpan> spans)
        {
            if (spans.Skip(1).Any())
            {
                return false;
            }

            var firstSpan = spans.First();
            return firstSpan.Contains(nodeSpan);
        }

        private async Task<Document> IterateAllCodeCleanupProvidersAsync(
            Document originalDocument,
            Document annotatedDocument,
            Func<SyntaxNode, IEnumerable<TextSpan>> spanGetter,
            IEnumerable<ICodeCleanupProvider> codeCleaners,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_IterateAllCodeCleanupProviders, cancellationToken))
            {
                var currentDocument = annotatedDocument;
                Document previousDocument = null;
                IEnumerable<TextSpan> spans = null;

#if DEBUG
                bool originalDocHasErrors = await annotatedDocument.HasAnyErrorsAsync(cancellationToken).ConfigureAwait(false);
#endif

                var current = 0;
                var count = codeCleaners.Count();

                foreach (var codeCleaner in codeCleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    current++;
                    if (previousDocument != currentDocument)
                    {
                        // Document was changed by the previous code cleaner, compute new spans.
                        var root = await currentDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        previousDocument = currentDocument;
                        spans = spanGetter(root);
                    }

                    // If we are at the end and there were no changes to the document, use the original document for the cleanup.
                    if (current == count && currentDocument == annotatedDocument)
                    {
                        currentDocument = originalDocument;
                    }

                    using (Logger.LogBlock(FunctionId.CodeCleanup_IterateOneCodeCleanup, GetCodeCleanerTypeName, codeCleaner, cancellationToken))
                    {
                        currentDocument = await codeCleaner.CleanupAsync(currentDocument, spans, cancellationToken).ConfigureAwait(false);
                    }

#if DEBUG
                    if (!originalDocHasErrors && currentDocument != annotatedDocument)
                    {
                        await currentDocument.VerifyNoErrorsAsync("Pretty-listing introduced errors in error-free code", cancellationToken).ConfigureAwait(false);
                    }
#endif
                }

                // If none of the cleanup operations changed the document, we should return the original document
                // rather than the one that has our annotations.
                if (currentDocument != annotatedDocument)
                {
                    return currentDocument;
                }
                else
                {
                    return originalDocument;
                }
            }
        }

        private async Task<SyntaxNode> IterateAllCodeCleanupProvidersAsync(
            SyntaxNode originalRoot,
            SyntaxNode annotatedRoot,
            Func<SyntaxNode, IEnumerable<TextSpan>> spanGetter,
            Workspace workspace,
            IEnumerable<ICodeCleanupProvider> codeCleaners,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_IterateAllCodeCleanupProviders, cancellationToken))
            {
                var currentRoot = annotatedRoot;
                SyntaxNode previousRoot = null;
                IEnumerable<TextSpan> spans = null;

                var current = 0;
                var count = codeCleaners.Count();

                foreach (var codeCleaner in codeCleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    current++;
                    if (previousRoot != currentRoot)
                    {
                        // The root was changed by the previous code cleaner, compute new spans.
                        previousRoot = currentRoot;
                        spans = spanGetter(currentRoot);
                    }

                    // If we are at the end and there were no changes to the document, use the original document for the cleanup.
                    if (current == count && currentRoot == annotatedRoot)
                    {
                        currentRoot = originalRoot;
                    }

                    using (Logger.LogBlock(FunctionId.CodeCleanup_IterateOneCodeCleanup, GetCodeCleanerTypeName, codeCleaner, cancellationToken))
                    {
                        currentRoot = await codeCleaner.CleanupAsync(currentRoot, spans, workspace, cancellationToken).ConfigureAwait(false);
                    }
                }

                // If none of the cleanup operations changed the root, we should return the original root
                // rather than the one that has our annotations.
                if (currentRoot != annotatedRoot)
                {
                    return currentRoot;
                }
                else
                {
                    return originalRoot;
                }
            }
        }

        private string GetCodeCleanerTypeName(ICodeCleanupProvider codeCleaner)
        {
            return codeCleaner.ToString();
        }

        private SyntaxNode InjectAnnotations(SyntaxNode node, Dictionary<SyntaxToken, List<SyntaxAnnotation>> map)
        {
            var tokenMap = map.ToDictionary(p => p.Key, p => p.Value);
            return node.ReplaceTokens(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o].ToArray()));
        }

        private bool TryCreateTextSpan(int start, int end, out TextSpan span)
        {
            span = default(TextSpan);

            if (start < 0 || end < start)
            {
                return false;
            }

            span = TextSpan.FromBounds(start, end);
            return true;
        }

        /// <summary>
        /// Enum that indicates type of span marker
        /// </summary>
        private enum SpanMarkerType
        {
            /// <summary>
            /// Normal case
            /// </summary>
            Normal,

            /// <summary>
            /// Span starts at the beginning of the tree
            /// </summary>
            BeginningOfFile,

            /// <summary>
            /// Span ends at the end of the tree
            /// </summary>
            EndOfFile
        }

        /// <summary>
        /// Internal annotation type to mark span location in the tree.
        /// </summary>
        private class SpanMarker
        {
            /// <summary>
            /// Indicates the current marker type
            /// </summary>
            public SpanMarkerType Type { get; }

            /// <summary>
            /// Indicates how to find the other side of the span marker if it is missing
            /// </summary>
            public SpanMarkerType OppositeMarkerType { get; }

            public SyntaxAnnotation Annotation { get; }

            public static readonly string AnnotationId = "SpanMarker";

            private SpanMarker(SpanMarkerType type, SpanMarkerType oppositeMarkerType, SyntaxAnnotation annotation)
            {
                this.Type = type;
                this.OppositeMarkerType = oppositeMarkerType;
                this.Annotation = annotation;
            }

            public SpanMarker(SpanMarkerType type = SpanMarkerType.Normal, SpanMarkerType oppositeMarkerType = SpanMarkerType.Normal)
                : this(type, oppositeMarkerType, new SyntaxAnnotation(AnnotationId, string.Format("{0} {1}", type, oppositeMarkerType)))
            {
            }

            private static readonly char[] s_separators = new char[] { ' ' };

            public static SpanMarker FromAnnotation(SyntaxAnnotation annotation)
            {
                var types = annotation.Data.Split(s_separators).Select(s => (SpanMarkerType)Enum.Parse(typeof(SpanMarkerType), s)).ToArray();
                return new SpanMarker(types[0], types[1], annotation);
            }
        }
    }
}
