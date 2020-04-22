// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal readonly struct ComplexifiedSpan
    {
        public readonly TextSpan OriginalSpan;
        public readonly TextSpan NewSpan;
        public readonly ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> ModifiedSubSpans;

        public ComplexifiedSpan(TextSpan originalSpan, TextSpan newSpan, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> modifiedSubSpans)
        {
            OriginalSpan = originalSpan;
            NewSpan = newSpan;
            ModifiedSubSpans = modifiedSubSpans;
        }
    }

    internal readonly struct ConflictResolution
    {
        public readonly Solution NewSolution;
        public readonly Solution OldSolution;

        public readonly ImmutableArray<RelatedLocation> RelatedLocations;
        public readonly bool ReplacementTextValid;

        private readonly ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> _documentToModifiedSpansMap;
        private readonly ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> _documentToComplexifiedSpansMap;

        private readonly ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> _documentToRelatedLocationsMap;

        /// <summary>
        /// The list of all document ids of documents that have been touched for this rename operation.
        /// </summary>
        public readonly ImmutableArray<DocumentId> DocumentIds;

        public ConflictResolution(MutableConflictResolution resolution)
        {
            OldSolution = resolution.OldSolution;
            NewSolution = resolution.NewSolution;
            RelatedLocations = resolution.RelatedLocations.ToImmutableArray();
            ReplacementTextValid = resolution.ReplacementTextValid;

            DocumentIds = resolution.RenamedSpansTracker.DocumentIds.Concat(
                resolution.RelatedLocations.Select(l => l.DocumentId)).Distinct().ToImmutableArray();

            _documentToModifiedSpansMap = resolution.RenamedSpansTracker.GetDocumentToModifiedSpansMap();
            _documentToComplexifiedSpansMap = resolution.RenamedSpansTracker.GetDocumentToComplexifiedSpansMap();
            _documentToRelatedLocationsMap = resolution.RelatedLocations.GroupBy(loc => loc.DocumentId).ToImmutableDictionary(
                g => g.Key, g => g.ToImmutableArray());
        }

        public ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> GetComplexifiedSpans(DocumentId documentId)
            => _documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans)
                ? complexifiedSpans.SelectAsArray(c => (c.OriginalSpan, c.NewSpan))
                : ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>.Empty;

        public ImmutableDictionary<TextSpan, TextSpan> GetModifiedSpanMap(DocumentId documentId)
        {
            var result = ImmutableDictionary.CreateBuilder<TextSpan, TextSpan>();
            if (_documentToModifiedSpansMap.TryGetValue(documentId, out var modifiedSpans))
            {
                foreach (var (oldSpan, newSpan) in modifiedSpans)
                    result[oldSpan] = newSpan;
            }

            if (_documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
            {
                foreach (var complexifiedSpan in complexifiedSpans)
                {
                    foreach (var (oldSpan, newSpan) in complexifiedSpan.ModifiedSubSpans)
                        result[oldSpan] = newSpan;
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<RelatedLocation> GetRelatedLocationsForDocument(DocumentId documentId)
            => _documentToRelatedLocationsMap.TryGetValue(documentId, out var result)
                ? result
                : ImmutableArray<RelatedLocation>.Empty;

        internal TextSpan GetResolutionTextSpan(TextSpan originalSpan, DocumentId documentId)
        {
            if (_documentToModifiedSpansMap.TryGetValue(documentId, out var modifiedSpans))
            {
                var first = modifiedSpans.FirstOrNull(t => t.oldSpan == originalSpan);
                if (first.HasValue)
                    return first.Value.newSpan;
            }

            if (_documentToComplexifiedSpansMap.TryGetValue(documentId, out var complexifiedSpans))
            {
                var first = complexifiedSpans.FirstOrNull(c => c.OriginalSpan.Contains(originalSpan));
                if (first.HasValue)
                    return first.Value.NewSpan;
            }

            // The RenamedSpansTracker doesn't currently track unresolved conflicts for
            // unmodified locations.  If the document wasn't modified, we can just use the 
            // original span as the new span.
            return originalSpan;
        }
    }

    /// <summary>
    /// The result of the conflict engine. Once this object is returned from the engine, it is
    /// immutable.
    /// </summary>
    internal sealed class MutableConflictResolution
    {
        // Used to map spans from oldSolution to the newSolution
        public readonly RenamedSpansTracker RenamedSpansTracker;

        // List of All the Locations that were renamed and conflict-complexified
        public readonly List<RelatedLocation> RelatedLocations;

        /// <summary>
        /// The base workspace snapshot
        /// </summary>
        public readonly Solution OldSolution;

        /// <summary>
        /// Whether the text that was resolved with was even valid. This may be false if the
        /// identifier was not valid in some language that was involved in the rename.
        /// </summary>
        public readonly bool ReplacementTextValid;

        /// <summary>
        /// The original text that is the rename replacement.
        /// </summary>
        public readonly string ReplacementText;

        /// <summary>
        /// The new workspace snapshot
        /// </summary>
        public Solution NewSolution { get; private set; }

        public MutableConflictResolution(
            Solution oldSolution,
            RenamedSpansTracker renamedSpansTracker,
            string replacementText,
            bool replacementTextValid)
        {
            OldSolution = oldSolution;
            NewSolution = oldSolution;
            RenamedSpansTracker = renamedSpansTracker;
            ReplacementText = replacementText;
            ReplacementTextValid = replacementTextValid;
            RelatedLocations = new List<RelatedLocation>();
        }

        internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
        {
            RelatedLocations.RemoveAll(r => conflictLocationDocumentIds.Contains(r.DocumentId));
            RenamedSpansTracker.ClearDocuments(conflictLocationDocumentIds);
        }

        internal void UpdateCurrentSolution(Solution solution)
            => NewSolution = solution;

        internal async Task<Solution> RemoveAllRenameAnnotationsAsync(
            Solution intermediateSolution,
            IEnumerable<DocumentId> documentWithRenameAnnotations,
            AnnotationTable<RenameAnnotation> annotationSet,
            CancellationToken cancellationToken)
        {
            foreach (var documentId in documentWithRenameAnnotations)
            {
                if (RenamedSpansTracker.IsDocumentChanged(documentId))
                {
                    var document = NewSolution.GetDocument(documentId);
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    // For the computeReplacementToken and computeReplacementNode functions, use 
                    // the "updated" node to maintain any annotation removals from descendants.
                    var newRoot = root.ReplaceSyntax(
                        nodes: annotationSet.GetAnnotatedNodes(root),
                        computeReplacementNode: (original, updated) => annotationSet.WithoutAnnotations(updated, annotationSet.GetAnnotations(updated).ToArray()),
                        tokens: annotationSet.GetAnnotatedTokens(root),
                        computeReplacementToken: (original, updated) => annotationSet.WithoutAnnotations(updated, annotationSet.GetAnnotations(updated).ToArray()),
                        trivia: SpecializedCollections.EmptyEnumerable<SyntaxTrivia>(),
                        computeReplacementTrivia: null);

                    intermediateSolution = intermediateSolution.WithDocumentSyntaxRoot(documentId, newRoot, PreservationMode.PreserveIdentity);
                }
            }

            return intermediateSolution;
        }

        internal void RenameDocumentToMatchNewSymbol(Document document)
        {
            var extension = Path.GetExtension(document.Name);
            var newName = Path.ChangeExtension(ReplacementText, extension);

            NewSolution = NewSolution.WithDocumentName(document.Id, newName);
        }

        public int GetAdjustedTokenStartingPosition(int startingPosition, DocumentId documentId)
            => RenamedSpansTracker.GetAdjustedPosition(startingPosition, documentId);

        internal void AddRelatedLocation(RelatedLocation location)
            => RelatedLocations.Add(location);

        internal void AddOrReplaceRelatedLocation(RelatedLocation location)
        {
            var existingRelatedLocation = RelatedLocations.Where(rl => rl.ConflictCheckSpan == location.ConflictCheckSpan && rl.DocumentId == location.DocumentId).FirstOrNull();
            if (existingRelatedLocation != null)
                RelatedLocations.Remove(existingRelatedLocation.Value);

            AddRelatedLocation(location);
        }
    }
}
