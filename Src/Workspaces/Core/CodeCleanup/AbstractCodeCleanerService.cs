// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal abstract class AbstractCodeCleanerService : ICodeCleanerService
    {
        public abstract IEnumerable<ICodeCleanupProvider> GetDefaultProviders();

        public async Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FeatureId.CodeCleanup, FunctionId.CodeCleanup_CleanupAsync, cancellationToken))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // no span to format
                if (!spans.Any())
                {
                    // return as it is
                    return document;
                }

                var codeCleaners = providers ?? GetDefaultProviders();

                var normalizedSpan = spans.ToNormalizedSpans();
                if (CleanupWholeNode(root.FullSpan, normalizedSpan))
                {
                    // we are cleaning up whole document, no need to do expansive span tracking between cleaners
                    return await IterateAllCodeCleanupProvidersAsync(document, document, r => SpecializedCollections.SingletonEnumerable(r.FullSpan), codeCleaners, cancellationToken).ConfigureAwait(false);
                }

                var syntaxFactsService = LanguageService.GetService<ISyntaxFactsService>(document);
                Contract.Requires(syntaxFactsService != null);

                // we need to track spans between cleaners. annotate tree with provided spans
                var newNodeAndAnnotations = AnnotateNodeForTextSpans(syntaxFactsService, root, normalizedSpan, cancellationToken);

                // turns out we don't need to annotate anything since all spans are merged to one that covers whole node
                if (newNodeAndAnnotations.Item1 == null)
                {
                    // we are cleaning up whole document, no need to do expansive span tracking between cleaners
                    return await IterateAllCodeCleanupProvidersAsync(document, document, n => SpecializedCollections.SingletonEnumerable(n.FullSpan), codeCleaners, cancellationToken).ConfigureAwait(false);
                }

                var model = await document.GetSemanticModelForSpanAsync(spans.Collapse(), cancellationToken).ConfigureAwait(false);

                // replace initial node and document with annotated node
                var annotatedRoot = newNodeAndAnnotations.Item1;
                var annotatedDocument = document.WithSyntaxRoot(annotatedRoot);

                // run actual cleanup
                return await IterateAllCodeCleanupProvidersAsync(document, annotatedDocument, r => GetTextSpansFromAnnotation(r, newNodeAndAnnotations.Item2, cancellationToken), codeCleaners, cancellationToken).ConfigureAwait(false);
            }
        }

        public SyntaxNode Cleanup(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, IEnumerable<ICodeCleanupProvider> providers, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FeatureId.CodeCleanup, FunctionId.CodeCleanup_Cleanup, cancellationToken))
            {
                // no span to format
                if (!spans.Any())
                {
                    // return as it is
                    return root;
                }

                var codeCleaners = providers ?? GetDefaultProviders();

                var normalizedSpan = spans.ToNormalizedSpans();
                if (CleanupWholeNode(root.FullSpan, normalizedSpan))
                {
                    // we are cleaning up whole document, no need to do expansive span tracking between cleaners
                    return IterateAllCodeCleanupProviders(root, root, r => SpecializedCollections.SingletonEnumerable(r.FullSpan), workspace, codeCleaners, cancellationToken);
                }

                var syntaxFactsService = LanguageService.GetService<ISyntaxFactsService>(workspace, root.Language);
                Contract.Requires(syntaxFactsService != null);

                // we need to track spans between cleaners. annotate tree with provided spans
                var newNodeAndAnnotations = AnnotateNodeForTextSpans(syntaxFactsService, root, normalizedSpan, cancellationToken);

                // turns out we don't need to annotate anything since all spans are merged to one that covers whole node
                if (newNodeAndAnnotations.Item1 == null)
                {
                    // we are cleaning up whole document, no need to do expansive span tracking between cleaners
                    return IterateAllCodeCleanupProviders(root, root, n => SpecializedCollections.SingletonEnumerable(n.FullSpan), workspace, codeCleaners, cancellationToken);
                }

                // replace initial node and document with annotated node
                var annotatedRoot = newNodeAndAnnotations.Item1;

                // run actual cleanup
                return IterateAllCodeCleanupProviders(root, annotatedRoot, r => GetTextSpansFromAnnotation(r, newNodeAndAnnotations.Item2, cancellationToken), workspace, codeCleaners, cancellationToken);
            }
        }

        private IEnumerable<TextSpan> GetTextSpansFromAnnotation(SyntaxNode node, List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
        {
            // now try to retrieve text span from annotations injected to the node
            foreach (var annotationPair in annotations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var previousMarkerAnnotation = annotationPair.Item1;
                var nextMarkerAnnotation = annotationPair.Item2;

                var previousTokenMarker = SpanMarker.FromAnnotation(previousMarkerAnnotation);
                var nextTokenMarker = SpanMarker.FromAnnotation(nextMarkerAnnotation);

                var previousTokens = node.GetAnnotatedNodesAndTokens(previousMarkerAnnotation).Where(n => n.IsToken).Select(n => n.AsToken());
                var nextTokens = node.GetAnnotatedNodesAndTokens(nextMarkerAnnotation).Where(n => n.IsToken).Select(n => n.AsToken());

                // check whether we can use tokens we got back from the node
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
            // set initial value
            span = default(TextSpan);

            var previousToken = previousTokens.FirstOrDefault();
            var nextToken = nextTokens.FirstOrDefault();

            // define all variables required to determine state
            var hasNoPreviousToken = previousToken.RawKind == 0;
            var hasNoNextToken = nextToken.RawKind == 0;

            var hasMultiplePreviousToken = previousTokens.Skip(1).Any();
            var hasMultipleNextToken = nextTokens.Skip(1).Any();

            var hasOnePreviousToken = !hasNoPreviousToken && !hasMultiplePreviousToken;
            var hasOneNextToken = !hasNoNextToken && !hasMultipleNextToken;

            // normal case
            if (hasOnePreviousToken && hasOneNextToken)
            {
                return TryCreateTextSpan(GetPreviousTokenStartPosition(previousTokenMarker.Type, previousToken),
                                         GetNextTokenEndPosition(nextTokenMarker.Type, nextToken),
                                         out span);
            }

            // quick error check * we don't have any token, ignore this one.
            // this can't happen unless one of cleaners violated contract of only changing things inside of the provided span
            if (hasNoPreviousToken && hasNoNextToken)
            {
                return false;
            }

            // we don't have previous token, but we do have one next token
            // nextTokenMarker has hint to how to find opposite marker
            // if we can't find the other side, then it means one of cleaners has violated contract. ignore span.
            if (hasNoPreviousToken && hasOneNextToken)
            {
                if (nextTokenMarker.OppositeMarkerType == SpanMarkerType.BeginningOfFile)
                {
                    // okay, found right span
                    return TryCreateTextSpan(node.SpanStart, GetNextTokenEndPosition(nextTokenMarker.Type, nextToken), out span);
                }

                return false;
            }

            // one previous token but no next token with hint case
            if (hasOnePreviousToken && hasNoNextToken)
            {
                if (previousTokenMarker.OppositeMarkerType == SpanMarkerType.EndOfFile)
                {
                    // okay, found right span
                    return TryCreateTextSpan(GetPreviousTokenStartPosition(previousTokenMarker.Type, previousToken), node.Span.End, out span);
                }

                return false;
            }

            // now simple cases are done. now we need to deal with cases where annotation found more than one corresponding tokens
            // mostly it means one of cleaners violated contract. so we can just ignore the span except one cases where it involves beginning and end of tree
            Contract.ThrowIfFalse(hasMultiplePreviousToken || hasMultipleNextToken);

            // check whether it is one of special cases or not
            if (hasMultiplePreviousToken && previousTokenMarker.Type == SpanMarkerType.BeginningOfFile)
            {
                // okay, it is edge case, let's use start of node as beginning of the span
                span = TextSpan.FromBounds(node.SpanStart, GetNextTokenEndPosition(nextTokenMarker.Type, nextToken));
                return true;
            }

            if (hasMultipleNextToken && nextTokenMarker.Type == SpanMarkerType.EndOfFile)
            {
                // okay, it is edge case, let's use end of node as end of the span
                span = TextSpan.FromBounds(GetPreviousTokenStartPosition(previousTokenMarker.Type, previousToken), node.Span.End);
                return true;
            }

            // all other cases are invalid cases where one of code cleaner should have messed up things by moving around things it shouldn't move
            return false;
        }

        /// <summary>
        /// get proper start position based on span marker type
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
        /// get proper end position based on span marker type
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
        /// inject annotations to the node so that it can re-calculate spans for each code cleaners after each tree transformation
        /// </summary>
        private ValueTuple<SyntaxNode, List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>>> AnnotateNodeForTextSpans(
            ISyntaxFactsService syntaxFactsService, SyntaxNode root, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            // get spans where tokens around the spans are not overlapping with spans
            var nonOverlappingSpans = GetNonOverlappingSpans(syntaxFactsService, root, spans, cancellationToken);

            // build token annotation map
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

                // create marker to insert
                SpanMarker startMarker = new SpanMarker(type: (previousToken.RawKind == 0) ? SpanMarkerType.BeginningOfFile : SpanMarkerType.Normal,
                                                        oppositeMarkerType: (nextToken.RawKind == 0) ? SpanMarkerType.EndOfFile : SpanMarkerType.Normal);

                SpanMarker endMarker = new SpanMarker(type: (nextToken.RawKind == 0) ? SpanMarkerType.EndOfFile : SpanMarkerType.Normal,
                                                      oppositeMarkerType: (previousToken.RawKind == 0) ? SpanMarkerType.BeginningOfFile : SpanMarkerType.Normal);

                // set proper tokens
                previousToken = (previousToken.RawKind == 0) ? root.GetFirstToken(includeZeroWidth: true) : previousToken;
                nextToken = (nextToken.RawKind == 0) ? root.GetLastToken(includeZeroWidth: true) : nextToken;

                // build token to marker map
                tokenAnnotationMap.GetOrAdd(previousToken, _ => new List<SyntaxAnnotation>()).Add(startMarker.Annotation);
                tokenAnnotationMap.GetOrAdd(nextToken, _ => new List<SyntaxAnnotation>()).Add(endMarker.Annotation);

                // remember markers
                annotations.Add(new ValueTuple<SyntaxAnnotation, SyntaxAnnotation>(startMarker.Annotation, endMarker.Annotation));
            }

            // do a quick check
            // if, after all merges, spans are merged into one span that covers whole tree, return right away
            if (CleanupWholeNode(annotations))
            {
                // this will indicate that no annotation is needed
                return default(ValueTuple<SyntaxNode, List<ValueTuple<SyntaxAnnotation, SyntaxAnnotation>>>);
            }

            // inject annotations
            var newNode = InjectAnnotations(root, tokenAnnotationMap);
            return ValueTuple.Create(newNode, annotations);
        }

        /// <summary>
        /// make sure annotations are positioned outside of any spans. if not, merge two adjacent spans to one
        /// </summary>
        private IEnumerable<TextSpan> GetNonOverlappingSpans(ISyntaxFactsService syntaxFactsService, SyntaxNode root, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            // create interval tree for spans
            var intervalTree = SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, spans);

            // find tokens that are outside of spans
            var tokenSpans = new List<TextSpan>();
            foreach (var span in spans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SyntaxToken startToken;
                SyntaxToken endToken;
                TextSpan spanAlignedToTokens = GetSpanAlignedToTokens(syntaxFactsService, root, span, out startToken, out endToken);

                SyntaxToken previousToken = startToken.GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
                SyntaxToken nextToken = endToken.GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

                // make sure previous and next token we found are not overlap with any existing spans. if it does, merge two spans
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
        /// retrieve 4 tokens around span like below.
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
            // get tokens at the edges of the span
            startToken = root.FindToken(span.Start, findInsideTrivia: true);

            endToken = root.FindTokenFromEnd(span.End, findInsideTrivia: true);

            // there must be tokens at each edge
            Contract.ThrowIfTrue(startToken.RawKind == 0 || endToken.RawKind == 0);

            // get previous and next tokens around span
            previousToken = startToken.GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            nextToken = endToken.GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
        }

        /// <summary>
        /// adjust provided span to align to either token's start position or end position
        /// </summary>
        private TextSpan GetSpanAlignedToTokens(
            ISyntaxFactsService syntaxFactsService, SyntaxNode root, TextSpan span, out SyntaxToken startToken, out SyntaxToken endToken)
        {
            startToken = FindTokenOnLeftOfPosition(syntaxFactsService, root, span.Start);
            endToken = FindTokenOnRightOfPosition(syntaxFactsService, root, span.End);

            // found two different tokens
            if (startToken.Span.End <= endToken.SpanStart)
            {
                return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
            }
            else
            {
                // found one token inside of the span
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
            using (Logger.LogBlock(FeatureId.CodeCleanup, FunctionId.CodeCleanup_IterateAllCodeCleanupProviders, cancellationToken))
            {
                var currentDocument = annotatedDocument;
                Document previousDocument = null;
                IEnumerable<TextSpan> spans = null;

#if DEBUG
                bool originalDocHasErrors = await annotatedDocument.HasAnyErrors(cancellationToken).ConfigureAwait(false);
#endif

                var current = 0;
                var count = codeCleaners.Count();

                foreach (var codeCleaner in codeCleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    current++;
                    if (previousDocument != currentDocument)
                    {
                        // document was changed by the previous code cleaner, compute new spans.
                        var root = await currentDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        previousDocument = currentDocument;
                        spans = spanGetter(root);
                    }

                    // if until at the end, there was no change to the document, use the original document for the cleanup
                    if (current == count && currentDocument == annotatedDocument)
                    {
                        currentDocument = originalDocument;
                    }

                    using (Logger.LogBlock(FeatureId.CodeCleanup, FunctionId.CodeCleanup_IterateOneCodeCleanup, GetCodeCleanerTypeName, codeCleaner, cancellationToken))
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
                // rather than the one that has our annotations
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

        private SyntaxNode IterateAllCodeCleanupProviders(
            SyntaxNode originalRoot,
            SyntaxNode annotatedRoot,
            Func<SyntaxNode, IEnumerable<TextSpan>> spanGetter,
            Workspace workspace,
            IEnumerable<ICodeCleanupProvider> codeCleaners,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FeatureId.CodeCleanup, FunctionId.CodeCleanup_IterateAllCodeCleanupProviders, cancellationToken))
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
                        // root was changed by the previous code cleaner, compute new spans.
                        previousRoot = currentRoot;
                        spans = spanGetter(currentRoot);
                    }

                    // if until at the end, there was no change to the document, use the original document for the cleanup
                    if (current == count && currentRoot == annotatedRoot)
                    {
                        currentRoot = originalRoot;
                    }

                    using (Logger.LogBlock(FeatureId.CodeCleanup, FunctionId.CodeCleanup_IterateOneCodeCleanup, GetCodeCleanerTypeName, codeCleaner, cancellationToken))
                    {
                        currentRoot = codeCleaner.Cleanup(currentRoot, spans, workspace, cancellationToken);
                    }
                }

                // If none of the cleanup operations changed the root, we should return the original root
                // rather than the one that has our annotations
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
        /// enum that indicates type of span marker
        /// </summary>
        [Serializable]
        private enum SpanMarkerType
        {
            /// <summary>
            /// normal case
            /// </summary>
            Normal,

            /// <summary>
            /// span starts from beginning of the tree
            /// </summary>
            BeginningOfFile,

            /// <summary>
            /// span ends to the end of the tree
            /// </summary>
            EndOfFile
        }

        /// <summary>
        /// internal annotation type to mark span location in the tree
        /// </summary>
        private class SpanMarker
        {
            /// <summary>
            /// indicate current marker type
            /// </summary>
            public SpanMarkerType Type { get; private set; }

            /// <summary>
            /// indicate how to find the other side of span marker if it is missing
            /// </summary>
            public SpanMarkerType OppositeMarkerType { get; private set; }

            public SyntaxAnnotation Annotation { get; private set; }

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

            private static readonly char[] separators = new char[] { ' ' };

            public static SpanMarker FromAnnotation(SyntaxAnnotation annotation)
            {
                var types = annotation.Data.Split(separators).Select(s => (SpanMarkerType)Enum.Parse(typeof(SpanMarkerType), s)).ToArray();
                return new SpanMarker(types[0], types[1], annotation);
            }
        }
    }
}
