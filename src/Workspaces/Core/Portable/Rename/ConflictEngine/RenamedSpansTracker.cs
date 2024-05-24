// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

/// <summary>
/// Tracks the text spans that were modified as part of a rename operation
/// </summary>
internal sealed class RenamedSpansTracker
{
    private readonly Dictionary<DocumentId, List<(TextSpan oldSpan, TextSpan newSpan)>> _documentToModifiedSpansMap = [];
    private readonly Dictionary<DocumentId, List<MutableComplexifiedSpan>> _documentToComplexifiedSpansMap = [];

    internal bool IsDocumentChanged(DocumentId documentId)
        => _documentToModifiedSpansMap.ContainsKey(documentId) || _documentToComplexifiedSpansMap.ContainsKey(documentId);

    internal void AddModifiedSpan(DocumentId documentId, TextSpan oldSpan, TextSpan newSpan)
    {
        if (!_documentToModifiedSpansMap.TryGetValue(documentId, out var spans))
        {
            spans = [];
            _documentToModifiedSpansMap[documentId] = spans;
        }

        spans.Add((oldSpan, newSpan));
    }

    internal void AddComplexifiedSpan(DocumentId documentId, TextSpan oldSpan, TextSpan newSpan, List<(TextSpan oldSpan, TextSpan newSpan)> modifiedSubSpans)
    {
        if (!_documentToComplexifiedSpansMap.TryGetValue(documentId, out var spans))
        {
            spans = [];
            _documentToComplexifiedSpansMap[documentId] = spans;
        }

        spans.Add(new MutableComplexifiedSpan(originalSpan: oldSpan, newSpan: newSpan, modifiedSubSpans: modifiedSubSpans));
    }

    // Given a position in the old solution, we get back the new adjusted position 
    internal int GetAdjustedPosition(int startingPosition, DocumentId documentId)
    {
        var documentReplacementSpans = _documentToModifiedSpansMap.TryGetValue(documentId, out var modifiedSpans)
            ? modifiedSpans.Where(pair => pair.oldSpan.Start < startingPosition)
            : [];

        var adjustedStartingPosition = startingPosition;
        foreach (var (oldSpan, newSpan) in documentReplacementSpans)
        {
            adjustedStartingPosition += newSpan.Length - oldSpan.Length;
        }

        var documentComplexifiedSpans = _documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans)
            ? complexifiedSpans.Where(c => c.OriginalSpan.Start <= startingPosition)
            : [];

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
    private class MutableComplexifiedSpan(
        TextSpan originalSpan, TextSpan newSpan, List<(TextSpan oldSpan, TextSpan newSpan)> modifiedSubSpans)
    {
        public TextSpan OriginalSpan = originalSpan;
        public TextSpan NewSpan = newSpan;
        public List<(TextSpan oldSpan, TextSpan newSpan)> ModifiedSubSpans = modifiedSubSpans;
    }

    internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
    {
        foreach (var documentId in conflictLocationDocumentIds)
        {
            _documentToModifiedSpansMap.Remove(documentId);
            _documentToComplexifiedSpansMap.Remove(documentId);
        }
    }

    public IEnumerable<DocumentId> DocumentIds
    {
        get
        {
            return _documentToModifiedSpansMap.Keys.Concat(_documentToComplexifiedSpansMap.Keys).Distinct();
        }
    }

    internal async Task<Solution> SimplifyAsync(Solution solution, IEnumerable<DocumentId> documentIds, bool replacementTextValid, AnnotationTable<RenameAnnotation> renameAnnotations, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        foreach (var documentId in documentIds)
        {
            if (this.IsDocumentChanged(documentId))
            {
                var document = solution.GetRequiredDocument(documentId);

                if (replacementTextValid)
                {
                    var cleanupOptions = await document.GetCodeCleanupOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

                    document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
                    document = await Formatter.FormatAsync(document, Formatter.Annotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);
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

                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // First, get all the complexified statements
                var nodeAnnotations = renameAnnotations.GetAnnotatedNodesAndTokens<RenameNodeSimplificationAnnotation>(root)
                    .Select(x => Tuple.Create(renameAnnotations.GetAnnotations<RenameNodeSimplificationAnnotation>(x).First(), (SyntaxNode)x!));

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

    public ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> GetDocumentToModifiedSpansMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>>();

        foreach (var (docId, spans) in _documentToModifiedSpansMap)
            builder.Add(docId, [.. spans]);

        return builder.ToImmutable();
    }

    public ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> GetDocumentToComplexifiedSpansMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<ComplexifiedSpan>>();

        foreach (var (docId, spans) in _documentToComplexifiedSpansMap)
        {
            builder.Add(docId, spans.SelectAsArray(
                s => new ComplexifiedSpan(s.OriginalSpan, s.NewSpan, [.. s.ModifiedSubSpans])));
        }

        return builder.ToImmutable();
    }
}
