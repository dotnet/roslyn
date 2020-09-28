// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// The result of the conflict engine. Can be made immutable by calling <see cref="ToConflictResolution()"/>.
    /// </summary>
    internal sealed class MutableConflictResolution
    {
        public readonly string ErrorMessage;

        // Used to map spans from oldSolution to the newSolution
        private readonly RenamedSpansTracker _renamedSpansTracker;

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
        /// The solution snapshot as it is being updated with specific rename steps.
        /// </summary>
        public Solution CurrentSolution { get; private set; }

        private (DocumentId documentId, string newName) _renamedDocument;

        public MutableConflictResolution(string errorMessage)
            => ErrorMessage = errorMessage;

        public MutableConflictResolution(
            Solution oldSolution,
            RenamedSpansTracker renamedSpansTracker,
            string replacementText,
            bool replacementTextValid)
        {
            OldSolution = oldSolution;
            CurrentSolution = oldSolution;
            _renamedSpansTracker = renamedSpansTracker;
            ReplacementText = replacementText;
            ReplacementTextValid = replacementTextValid;
            RelatedLocations = new List<RelatedLocation>();
        }

        internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
        {
            RelatedLocations.RemoveAll(r => conflictLocationDocumentIds.Contains(r.DocumentId));
            _renamedSpansTracker.ClearDocuments(conflictLocationDocumentIds);
        }

        internal void UpdateCurrentSolution(Solution solution)
            => CurrentSolution = solution;

        internal async Task<Solution> RemoveAllRenameAnnotationsAsync(
            Solution intermediateSolution,
            IEnumerable<DocumentId> documentWithRenameAnnotations,
            AnnotationTable<RenameAnnotation> annotationSet,
            CancellationToken cancellationToken)
        {
            foreach (var documentId in documentWithRenameAnnotations)
            {
                if (_renamedSpansTracker.IsDocumentChanged(documentId))
                {
                    var document = CurrentSolution.GetDocument(documentId);
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

            _renamedDocument = (document.Id, newName);
        }

        public int GetAdjustedTokenStartingPosition(int startingPosition, DocumentId documentId)
            => _renamedSpansTracker.GetAdjustedPosition(startingPosition, documentId);

        internal void AddRelatedLocation(RelatedLocation location)
            => RelatedLocations.Add(location);

        internal void AddOrReplaceRelatedLocation(RelatedLocation location)
        {
            var existingRelatedLocation = RelatedLocations.Where(rl => rl.ConflictCheckSpan == location.ConflictCheckSpan && rl.DocumentId == location.DocumentId).FirstOrNull();
            if (existingRelatedLocation != null)
                RelatedLocations.Remove(existingRelatedLocation.Value);

            AddRelatedLocation(location);
        }

        public ConflictResolution ToConflictResolution()
        {
            if (ErrorMessage != null)
                return new ConflictResolution(ErrorMessage);

            var documentIds = this._renamedSpansTracker.DocumentIds.Concat(
                this.RelatedLocations.Select(l => l.DocumentId)).Distinct().ToImmutableArray();

            var relatedLocations = this.RelatedLocations.ToImmutableArray();

            var documentToModifiedSpansMap = this._renamedSpansTracker.GetDocumentToModifiedSpansMap();
            var documentToComplexifiedSpansMap = this._renamedSpansTracker.GetDocumentToComplexifiedSpansMap();
            var documentToRelatedLocationsMap = this.RelatedLocations.GroupBy(loc => loc.DocumentId).ToImmutableDictionary(
                g => g.Key, g => g.ToImmutableArray());

            return new ConflictResolution(
                OldSolution,
                CurrentSolution,
                ReplacementTextValid,
                _renamedDocument,
                documentIds,
                relatedLocations,
                documentToModifiedSpansMap,
                documentToComplexifiedSpansMap,
                documentToRelatedLocationsMap);
        }
    }
}
