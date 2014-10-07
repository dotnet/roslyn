// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
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
        private readonly RenamedSpansTracker renamedSpansTracker;

        // List of All the Locations that were renamed and conflict-complexified
        private readonly List<RelatedLocation> relatedLocations;
        private readonly Solution oldSolution;
        private Solution newSolution;

        // This solution is updated after we finish processing each project.  It will only contain
        // documents that were modified with text changes (not the ones that were only annotated)
        private Solution intermediateSolutionContainingOnlyModifiedDocuments;

        // This is Lazy Initialized when it is first used
        private ILookup<DocumentId, RelatedLocation> relatedLocationsByDocumentId = null;

        public ConflictResolution(
            Solution oldSolution,
            RenamedSpansTracker renamedSpansTracker,
            string replacementText,
            bool replacementTextValid)
        {
            this.oldSolution = oldSolution;
            this.newSolution = oldSolution;
            this.intermediateSolutionContainingOnlyModifiedDocuments = oldSolution;
            this.renamedSpansTracker = renamedSpansTracker;
            ReplacementText = replacementText;
            ReplacementTextValid = replacementTextValid;
            relatedLocations = new List<RelatedLocation>();
        }

        internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
        {
            this.relatedLocations.RemoveAll(r => conflictLocationDocumentIds.Contains(r.DocumentId));
            this.renamedSpansTracker.ClearDocuments(conflictLocationDocumentIds);
        }

        internal void UpdateCurrentSolution(Solution solution)
        {
            this.newSolution = solution;
        }

        internal void RemoveAllRenameAnnotations(IEnumerable<DocumentId> documentWithRenameAnnotations, AnnotationTable<RenameAnnotation> annotationSet, CancellationToken cancellationToken)
        {
            foreach (var documentId in documentWithRenameAnnotations)
            {
                if (this.renamedSpansTracker.IsDocumentChanged(documentId))
                {
                    var document = newSolution.GetDocument(documentId);
                    var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                    var newRoot = root.ReplaceSyntax(
                        annotationSet.GetAnnotatedNodes(root),
                        (original, dummy) => annotationSet.WithoutAnnotations(original, annotationSet.GetAnnotations(original).ToArray()),
                        annotationSet.GetAnnotatedTokens(root),
                        (original, dummy) => annotationSet.WithoutAnnotations(original, annotationSet.GetAnnotations(original).ToArray()),
                        SpecializedCollections.EmptyEnumerable<SyntaxTrivia>(),
                        computeReplacementTrivia: null);

                    this.intermediateSolutionContainingOnlyModifiedDocuments = this.intermediateSolutionContainingOnlyModifiedDocuments.WithDocumentSyntaxRoot(documentId, newRoot);
                }
            }

            this.newSolution = this.intermediateSolutionContainingOnlyModifiedDocuments;
        }

        /// <summary>
        /// The list of all symbol locations that are referenced either by the original symbol or
        /// the renamed symbol. This includes both resolved and unresolved conflicts.
        /// </summary>
        public IList<RelatedLocation> RelatedLocations
        {
            get
            {
                return new ReadOnlyCollection<RelatedLocation>(relatedLocations);
            }
        }

        public RenamedSpansTracker RenamedSpansTracker
        {
            get
            {
                return this.renamedSpansTracker;
            }
        }

        public int GetAdjustedTokenStartingPosition(
            int startingPosition,
            DocumentId documentId)
        {
            return renamedSpansTracker.GetAdjustedPosition(startingPosition, documentId);
        }

        // test hook only
        public TextSpan GetResolutionTextSpan(
            TextSpan originalSpan,
            DocumentId documentId)
        {
            return renamedSpansTracker.GetResolutionTextSpan(originalSpan, documentId);
        }

        /// <summary>
        /// The list of all document ids of documents that have been touched for this rename operation.
        /// </summary>
        public IEnumerable<DocumentId> DocumentIds
        {
            get
            {
                return this.renamedSpansTracker.DocumentIds;
            }
        }

        public IEnumerable<RelatedLocation> GetRelatedLocationsForDocument(DocumentId documentId)
        {
            if (this.relatedLocationsByDocumentId == null)
            {
                this.relatedLocationsByDocumentId = this.relatedLocations.ToLookup(r => r.DocumentId);
            }

            if (this.relatedLocationsByDocumentId.Contains(documentId))
            {
                return this.relatedLocationsByDocumentId[documentId];
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<RelatedLocation>();
            }
        }

        internal void AddRelatedLocation(RelatedLocation location)
        {
            relatedLocations.Add(location);
        }

        internal void AddOrReplaceRelatedLocation(RelatedLocation location)
        {
            var existingRelatedLocation = relatedLocations.Where(rl => rl.ConflictCheckSpan == location.ConflictCheckSpan && rl.DocumentId == location.DocumentId).FirstOrDefault();
            if (existingRelatedLocation != null)
            {
                relatedLocations.Remove(existingRelatedLocation);
            }

            AddRelatedLocation(location);
        }

        /// <summary>
        /// The new workspace snapshot
        /// </summary>
        public Solution NewSolution
        {
            get
            {
                return newSolution;
            }
        }

        /// <summary>
        /// The base workspace snapshot
        /// </summary>
        public Solution OldSolution
        {
            get
            {
                return oldSolution;
            }
        }

        /// <summary>
        /// Whether the text that was resolved with was even valid. This may be false if the
        /// identifier was not valid in some language that was involved in the rename.
        /// </summary>
        public bool ReplacementTextValid { get; private set; }

        /// <summary>
        /// The original text that is the rename replacement.
        /// </summary>
        public string ReplacementText { get; private set; }
    }
}
