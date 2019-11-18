// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// The result of the conflict engine. Once this object is returned from the engine, it is
    /// immutable.
    /// </summary>
    internal sealed class ConflictResolution
    {
        // Used to map spans from oldSolution to the newSolution
        private readonly RenamedSpansTracker _renamedSpansTracker;

        // List of All the Locations that were renamed and conflict-complexified
        private readonly List<RelatedLocation> _relatedLocations;
        private readonly Solution _oldSolution;
        private Solution _newSolution;

        // This solution is updated after we finish processing each project.  It will only contain
        // documents that were modified with text changes (not the ones that were only annotated)
        private Solution _intermediateSolutionContainingOnlyModifiedDocuments;

        // This is Lazy Initialized when it is first used
        private ILookup<DocumentId, RelatedLocation> _relatedLocationsByDocumentId;

        public ConflictResolution(
            Solution oldSolution,
            RenamedSpansTracker renamedSpansTracker,
            string replacementText,
            bool replacementTextValid)
        {
            _oldSolution = oldSolution;
            _newSolution = oldSolution;
            _intermediateSolutionContainingOnlyModifiedDocuments = oldSolution;
            _renamedSpansTracker = renamedSpansTracker;
            ReplacementText = replacementText;
            ReplacementTextValid = replacementTextValid;
            _relatedLocations = new List<RelatedLocation>();
        }

        internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
        {
            _relatedLocations.RemoveAll(r => conflictLocationDocumentIds.Contains(r.DocumentId));
            _renamedSpansTracker.ClearDocuments(conflictLocationDocumentIds);
        }

        internal void UpdateCurrentSolution(Solution solution)
        {
            _newSolution = solution;
        }

        internal async Task RemoveAllRenameAnnotationsAsync(IEnumerable<DocumentId> documentWithRenameAnnotations, AnnotationTable<RenameAnnotation> annotationSet, CancellationToken cancellationToken)
        {
            foreach (var documentId in documentWithRenameAnnotations)
            {
                if (_renamedSpansTracker.IsDocumentChanged(documentId))
                {
                    var document = _newSolution.GetDocument(documentId);
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

                    _intermediateSolutionContainingOnlyModifiedDocuments = _intermediateSolutionContainingOnlyModifiedDocuments.WithDocumentSyntaxRoot(documentId, newRoot, PreservationMode.PreserveIdentity);
                }
            }

            _newSolution = _intermediateSolutionContainingOnlyModifiedDocuments;
        }

        internal void RenameDocumentToMatchNewSymbol(Document document)
        {
            var extension = Path.GetExtension(document.Name);
            var newName = Path.ChangeExtension(ReplacementText, extension);

            _newSolution = _newSolution.WithDocumentName(document.Id, newName);
        }

        /// <summary>
        /// The list of all symbol locations that are referenced either by the original symbol or
        /// the renamed symbol. This includes both resolved and unresolved conflicts.
        /// </summary>
        public IList<RelatedLocation> RelatedLocations
        {
            get
            {
                return new ReadOnlyCollection<RelatedLocation>(_relatedLocations);
            }
        }

        public RenamedSpansTracker RenamedSpansTracker => _renamedSpansTracker;

        public int GetAdjustedTokenStartingPosition(
            int startingPosition,
            DocumentId documentId)
        {
            return _renamedSpansTracker.GetAdjustedPosition(startingPosition, documentId);
        }

        /// <summary>
        /// The list of all document ids of documents that have been touched for this rename operation.
        /// </summary>
        public IEnumerable<DocumentId> DocumentIds => _renamedSpansTracker.DocumentIds;

        public IEnumerable<RelatedLocation> GetRelatedLocationsForDocument(DocumentId documentId)
        {
            if (_relatedLocationsByDocumentId == null)
            {
                _relatedLocationsByDocumentId = _relatedLocations.ToLookup(r => r.DocumentId);
            }

            if (_relatedLocationsByDocumentId.Contains(documentId))
            {
                return _relatedLocationsByDocumentId[documentId];
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<RelatedLocation>();
            }
        }

        internal void AddRelatedLocation(RelatedLocation location)
        {
            _relatedLocations.Add(location);
        }

        internal void AddOrReplaceRelatedLocation(RelatedLocation location)
        {
            var existingRelatedLocation = _relatedLocations.Where(rl => rl.ConflictCheckSpan == location.ConflictCheckSpan && rl.DocumentId == location.DocumentId).FirstOrDefault();
            if (existingRelatedLocation != null)
            {
                _relatedLocations.Remove(existingRelatedLocation);
            }

            AddRelatedLocation(location);
        }

        /// <summary>
        /// The new workspace snapshot
        /// </summary>
        public Solution NewSolution => _newSolution;

        /// <summary>
        /// The base workspace snapshot
        /// </summary>
        public Solution OldSolution => _oldSolution;

        /// <summary>
        /// Whether the text that was resolved with was even valid. This may be false if the
        /// identifier was not valid in some language that was involved in the rename.
        /// </summary>
        public bool ReplacementTextValid { get; }

        /// <summary>
        /// The original text that is the rename replacement.
        /// </summary>
        public string ReplacementText { get; }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly ConflictResolution _conflictResolution;

            public TestAccessor(ConflictResolution conflictResolution)
            {
                _conflictResolution = conflictResolution;
            }

            internal TextSpan GetResolutionTextSpan(
                TextSpan originalSpan,
                DocumentId documentId)
            {
                return _conflictResolution._renamedSpansTracker.GetTestAccessor().GetResolutionTextSpan(originalSpan, documentId);
            }
        }
    }
}
