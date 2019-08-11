// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Tracks the text spans that were modified as part of a rename operation
    /// </summary>
    internal sealed class RenamedSpansTracker
    {
        private readonly Dictionary<DocumentId, List<(TextSpan oldSpan, TextSpan newSpan)>> _documentToModifiedSpansMap;
        private readonly Dictionary<DocumentId, List<ComplexifiedSpan>> _documentToComplexifiedSpansMap;

        public RenamedSpansTracker()
        {
            _documentToComplexifiedSpansMap = new Dictionary<DocumentId, List<ComplexifiedSpan>>();
            _documentToModifiedSpansMap = new Dictionary<DocumentId, List<(TextSpan oldSpan, TextSpan newSpan)>>();
        }

        internal bool IsDocumentChanged(DocumentId documentId)
        {
            return _documentToModifiedSpansMap.ContainsKey(documentId) || _documentToComplexifiedSpansMap.ContainsKey(documentId);
        }

        internal void AddModifiedSpan(DocumentId documentId, TextSpan oldSpan, TextSpan newSpan)
        {
            if (!_documentToModifiedSpansMap.TryGetValue(documentId, out var spans))
            {
                spans = new List<(TextSpan oldSpan, TextSpan newSpan)>();
                _documentToModifiedSpansMap[documentId] = spans;
            }

            spans.Add((oldSpan, newSpan));
        }

        internal void AddComplexifiedSpan(DocumentId documentId, TextSpan oldSpan, TextSpan newSpan, List<(TextSpan oldSpan, TextSpan newSpan)> modifiedSubSpans)
        {
            if (!_documentToComplexifiedSpansMap.TryGetValue(documentId, out var spans))
            {
                spans = new List<ComplexifiedSpan>();
                _documentToComplexifiedSpansMap[documentId] = spans;
            }

            spans.Add(new ComplexifiedSpan() { OriginalSpan = oldSpan, NewSpan = newSpan, ModifiedSubSpans = modifiedSubSpans });
        }

        internal TextSpan GetAdjustedComplexifiedSpan(TextSpan originalComplexifiedSpan, DocumentId documentId)
        {
            if (!_documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
            {
                throw new ArgumentException("documentId");
            }

            foreach (var complexifiedSpan in complexifiedSpans)
            {
                if (complexifiedSpan.OriginalSpan == originalComplexifiedSpan)
                {
                    return complexifiedSpan.NewSpan;
                }
            }

            throw new ArgumentException("originalComplexifiedSpan");
        }

        // Given a position in the old solution, we get back the new adjusted position 
        internal int GetAdjustedPosition(int startingPosition, DocumentId documentId)
        {
            var documentReplacementSpans = _documentToModifiedSpansMap.ContainsKey(documentId)
                ? _documentToModifiedSpansMap[documentId].Where(pair => pair.oldSpan.Start < startingPosition) :
                SpecializedCollections.EmptyEnumerable<(TextSpan oldSpan, TextSpan newSpan)>();

            var adjustedStartingPosition = startingPosition;
            foreach (var (oldSpan, newSpan) in documentReplacementSpans)
            {
                adjustedStartingPosition += newSpan.Length - oldSpan.Length;
            }

            var documentComplexifiedSpans = _documentToComplexifiedSpansMap.ContainsKey(documentId)
            ? _documentToComplexifiedSpansMap[documentId].Where(c => c.OriginalSpan.Start <= startingPosition) :
            SpecializedCollections.EmptyEnumerable<ComplexifiedSpan>();

            var appliedTextSpans = new HashSet<TextSpan>();
            foreach (var c in documentComplexifiedSpans.Reverse())
            {
                if (startingPosition >= c.OriginalSpan.End && !appliedTextSpans.Any(s => s.Contains(c.OriginalSpan)))
                {
                    appliedTextSpans.Add(c.OriginalSpan);
                    adjustedStartingPosition += c.NewSpan.Length - c.OriginalSpan.Length;
                }
                else
                {
                    foreach (var (oldSpan, newSpan) in c.ModifiedSubSpans.OrderByDescending(t => t.oldSpan.Start))
                    {
                        if (!appliedTextSpans.Any(s => s.Contains(oldSpan)))
                        {
                            if (startingPosition == oldSpan.Start)
                            {
                                return startingPosition + newSpan.Start - oldSpan.Start;
                            }
                            else if (startingPosition > oldSpan.Start)
                            {
                                return startingPosition + newSpan.End - oldSpan.End;
                            }
                        }
                    }

                    // if we get here, the starting position passed in is in the middle of our complexified
                    // span at a position that wasn't modified during complexification.  
                }
            }

            return adjustedStartingPosition;
        }

        /// <summary>
        /// Information to track deltas of complexified spans
        /// 
        /// Consider the following example where renaming a->b causes a conflict 
        /// and Goo is an extension method:
        ///     "a.Goo(a)" is rewritten to "NS1.NS2.Goo(NS3.a, NS3.a)"
        /// 
        /// The OriginalSpan is the span of "a.Goo(a)"
        /// 
        /// The NewSpan is the span of "NS1.NS2.Goo(NS3.a, NS3.a)"
        /// 
        /// The ModifiedSubSpans are the pairs of complexified symbols sorted 
        /// according to their order in the original source code span:
        ///     "a", "NS3.a"
        ///     "Goo", "NS1.NS2.Goo"
        ///     "a", "NS3.a"
        /// 
        /// </summary>
        private class ComplexifiedSpan
        {
            public TextSpan OriginalSpan;
            public TextSpan NewSpan;
            public List<(TextSpan oldSpan, TextSpan newSpan)> ModifiedSubSpans;
        }

        internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
        {
            foreach (var documentId in conflictLocationDocumentIds)
            {
                _documentToModifiedSpansMap.Remove(documentId);
                _documentToComplexifiedSpansMap.Remove(documentId);
            }
        }

        internal bool ContainsResolvedNonReferenceLocation(DocumentId documentId, TextSpan originalLocation)
        {
            return // (this.documentToModifiedSpansMap.Contains(documentId) && this.documentToModifiedSpansMap[documentId].Contains(t => t.oldSpan == originalLocation.SourceSpan)) ||
                _documentToComplexifiedSpansMap.ContainsKey(documentId) &&
                _documentToComplexifiedSpansMap[documentId].Contains(c => c.OriginalSpan.Contains(originalLocation));
        }

        public IEnumerable<DocumentId> DocumentIds
        {
            get
            {
                return _documentToModifiedSpansMap.Keys.Concat(_documentToComplexifiedSpansMap.Keys).Distinct();
            }
        }

        internal async Task<Solution> SimplifyAsync(Solution solution, IEnumerable<DocumentId> documentIds, bool replacementTextValid, AnnotationTable<RenameAnnotation> renameAnnotations, CancellationToken cancellationToken)
        {
            foreach (var documentId in documentIds)
            {
                if (this.IsDocumentChanged(documentId))
                {
                    var document = solution.GetDocument(documentId);

                    if (replacementTextValid)
                    {
                        document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
                        document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }

                    // Simplification may have removed escaping and formatted whitespace.  We need to update
                    // our list of modified spans accordingly
                    if (_documentToModifiedSpansMap.TryGetValue(documentId, out var modifiedSpans))
                    {
                        modifiedSpans.Clear();
                    }

                    if (_documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
                    {
                        complexifiedSpans.Clear();
                    }

                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    // First, get all the complexified statements
                    var nodeAnnotations = renameAnnotations.GetAnnotatedNodesAndTokens<RenameNodeSimplificationAnnotation>(root)
                        .Select(x => Tuple.Create(renameAnnotations.GetAnnotations<RenameNodeSimplificationAnnotation>(x).First(), (SyntaxNode)x));

                    var modifiedTokensInComplexifiedStatements = new HashSet<SyntaxToken>();
                    foreach (var annotationAndNode in nodeAnnotations)
                    {
                        var oldSpan = annotationAndNode.Item1.OriginalTextSpan;
                        var node = annotationAndNode.Item2;

                        var annotationAndTokens2 = renameAnnotations.GetAnnotatedNodesAndTokens<RenameTokenSimplificationAnnotation>(node)
                               .Select(x => Tuple.Create(renameAnnotations.GetAnnotations<RenameTokenSimplificationAnnotation>(x).First(), (SyntaxToken)x));

                        var modifiedSubSpans = new List<(TextSpan oldSpan, TextSpan newSpan)>();
                        foreach (var annotationAndToken in annotationAndTokens2)
                        {
                            modifiedTokensInComplexifiedStatements.Add(annotationAndToken.Item2);
                            modifiedSubSpans.Add((annotationAndToken.Item1.OriginalTextSpan, annotationAndToken.Item2.Span));
                        }

                        AddComplexifiedSpan(documentId, oldSpan, node.Span, modifiedSubSpans);
                    }

                    // Now process the rest of the renamed spans
                    var annotationAndTokens = renameAnnotations.GetAnnotatedNodesAndTokens<RenameTokenSimplificationAnnotation>(root)
                        .Where(x => !modifiedTokensInComplexifiedStatements.Contains((SyntaxToken)x))
                        .Select(x => Tuple.Create(renameAnnotations.GetAnnotations<RenameTokenSimplificationAnnotation>(x).First(), (SyntaxToken)x));

                    foreach (var annotationAndToken in annotationAndTokens)
                    {
                        AddModifiedSpan(documentId, annotationAndToken.Item1.OriginalTextSpan, annotationAndToken.Item2.Span);
                    }

                    var annotationAndTrivias = renameAnnotations.GetAnnotatedTrivia<RenameTokenSimplificationAnnotation>(root)
                        .Select(x => Tuple.Create(renameAnnotations.GetAnnotations<RenameTokenSimplificationAnnotation>(x).First(), x));

                    foreach (var annotationAndTrivia in annotationAndTrivias)
                    {
                        AddModifiedSpan(documentId, annotationAndTrivia.Item1.OriginalTextSpan, annotationAndTrivia.Item2.Span);
                    }

                    solution = document.Project.Solution;
                }
            }

            return solution;
        }

        internal Dictionary<TextSpan, TextSpan> GetModifiedSpanMap(DocumentId documentId)
        {
            var result = new Dictionary<TextSpan, TextSpan>();
            if (_documentToModifiedSpansMap.TryGetValue(documentId, out var modifiedSpans))
            {
                foreach (var (oldSpan, newSpan) in modifiedSpans)
                {
                    result[oldSpan] = newSpan;
                }
            }

            if (_documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
            {
                foreach (var complexifiedSpan in complexifiedSpans)
                {
                    foreach (var (oldSpan, newSpan) in complexifiedSpan.ModifiedSubSpans)
                    {
                        result[oldSpan] = newSpan;
                    }
                }
            }

            return result;
        }

        internal IEnumerable<(TextSpan oldSpan, TextSpan newSpan)> GetComplexifiedSpans(DocumentId documentId)
        {
            if (_documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
            {
                return complexifiedSpans.Select(c => (c.OriginalSpan, c.NewSpan));
            }

            return SpecializedCollections.EmptyEnumerable<(TextSpan oldSpan, TextSpan newSpan)>();
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RenamedSpansTracker _renamedSpansTracker;

            public TestAccessor(RenamedSpansTracker renamedSpansTracker)
            {
                _renamedSpansTracker = renamedSpansTracker;
            }

            internal TextSpan GetResolutionTextSpan(TextSpan originalSpan, DocumentId documentId)
            {
                if (_renamedSpansTracker._documentToModifiedSpansMap.TryGetValue(documentId, out var modifiedSpans) &&
                    modifiedSpans.Contains(t => t.oldSpan == originalSpan))
                {
                    return modifiedSpans.First(t => t.oldSpan == originalSpan).newSpan;
                }

                if (_renamedSpansTracker._documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
                {
                    return complexifiedSpans.First(c => c.OriginalSpan.Contains(originalSpan)).NewSpan;
                }

                // The RenamedSpansTracker doesn't currently track unresolved conflicts for
                // unmodified locations.  If the document wasn't modified, we can just use the 
                // original span as the new span.
                return originalSpan;
            }
        }
    }
}
