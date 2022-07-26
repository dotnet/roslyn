// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal readonly partial struct ConflictResolution
    {
        /// <summary>
        /// A flag indicate if the rename operation is successful or not.
        /// If this is false, the <see cref="ErrorMessage"/> would be with this resolution. All the other field or property would be <see langword="null"/> or empty.
        /// If this is true, the <see cref="ErrorMessage"/> would be null. All the other fields or properties would be valid.
        /// </summary>
        [MemberNotNullWhen(false, nameof(ErrorMessage))]
        [MemberNotNullWhen(true, nameof(_newSolutionWithoutRenamedDocument))]
        [MemberNotNullWhen(true, nameof(OldSolution))]
        [MemberNotNullWhen(true, nameof(NewSolution))]
        public bool IsSuccessful { get; }

        public readonly string? ErrorMessage;

        private readonly Solution? _newSolutionWithoutRenamedDocument;
        private readonly ImmutableDictionary<DocumentId, string> _renamedDocuments;

        public readonly Solution? OldSolution;

        /// <summary>
        /// The final solution snapshot.  Including any renamed documents.
        /// </summary>
        public readonly Solution? NewSolution;

        public readonly ImmutableDictionary<ISymbol, bool> SymbolToReplacementTextValid;

        /// <summary>
        /// The list of all document ids of documents that have been touched for this rename operation.
        /// </summary>
        public readonly ImmutableArray<DocumentId> DocumentIds;

        public readonly ImmutableArray<RelatedLocation> RelatedLocations;

        private readonly ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> _documentToModifiedSpansMap;
        private readonly ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> _documentToComplexifiedSpansMap;
        private readonly ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> _documentToRelatedLocationsMap;

        public ConflictResolution(string errorMessage)
        {
            IsSuccessful = false;
            ErrorMessage = errorMessage;

            _newSolutionWithoutRenamedDocument = null;
            _renamedDocuments = ImmutableDictionary<DocumentId, string>.Empty;
            OldSolution = null;
            NewSolution = null;
            SymbolToReplacementTextValid = ImmutableDictionary<ISymbol, bool>.Empty;
            DocumentIds = ImmutableArray<DocumentId>.Empty;
            RelatedLocations = ImmutableArray<RelatedLocation>.Empty;
            _documentToModifiedSpansMap = ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>>.Empty;
            _documentToComplexifiedSpansMap = ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>>.Empty;
            _documentToRelatedLocationsMap = ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>>.Empty;
        }

        public ConflictResolution(
            Solution oldSolution,
            Solution newSolutionWithoutRenamedDocument,
            ImmutableDictionary<ISymbol, bool> symbolToReplacementTextValid,
            ImmutableDictionary<DocumentId, string> renamedDocuments,
            ImmutableArray<DocumentId> documentIds, ImmutableArray<RelatedLocation> relatedLocations,
            ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> documentToModifiedSpansMap,
            ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> documentToComplexifiedSpansMap,
            ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> documentToRelatedLocationsMap)
        {
            IsSuccessful = true;
            ErrorMessage = null;

            OldSolution = oldSolution;
            _newSolutionWithoutRenamedDocument = newSolutionWithoutRenamedDocument;
            SymbolToReplacementTextValid = symbolToReplacementTextValid;
            _renamedDocuments = renamedDocuments;
            DocumentIds = documentIds;
            RelatedLocations = relatedLocations;
            _documentToModifiedSpansMap = documentToModifiedSpansMap;
            _documentToComplexifiedSpansMap = documentToComplexifiedSpansMap;
            _documentToRelatedLocationsMap = documentToRelatedLocationsMap;

            NewSolution = _newSolutionWithoutRenamedDocument;

            if (!_renamedDocuments.IsEmpty)
            {
                foreach (var (documentId, newName) in _renamedDocuments)
                {
                    NewSolution = _newSolutionWithoutRenamedDocument.WithDocumentName(documentId, newName);
                }
            }
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
}
