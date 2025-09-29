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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

/// <summary>
/// The result of the conflict engine. Can be made immutable by calling <see cref="ToConflictResolution()"/>.
/// </summary>
internal sealed class MutableConflictResolution(
    Solution oldSolution,
    RenamedSpansTracker renamedSpansTracker,
    string replacementText,
    bool replacementTextValid)
{
    // List of All the Locations that were renamed and conflict-complexified
    public readonly List<RelatedLocation> RelatedLocations = [];

    /// <summary>
    /// The base workspace snapshot
    /// </summary>
    public readonly Solution OldSolution = oldSolution;

    /// <summary>
    /// Whether the text that was resolved with was even valid. This may be false if the
    /// identifier was not valid in some language that was involved in the rename.
    /// </summary>
    public readonly bool ReplacementTextValid = replacementTextValid;

    /// <summary>
    /// The original text that is the rename replacement.
    /// </summary>
    public readonly string ReplacementText = replacementText;

    /// <summary>
    /// The solution snapshot as it is being updated with specific rename steps.
    /// </summary>
    public Solution CurrentSolution { get; private set; } = oldSolution;

    private (DocumentId documentId, string newName) _renamedDocument;

    internal void ClearDocuments(IEnumerable<DocumentId> conflictLocationDocumentIds)
    {
        RelatedLocations.RemoveAll(r => conflictLocationDocumentIds.Contains(r.DocumentId));
        renamedSpansTracker.ClearDocuments(conflictLocationDocumentIds);
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
            if (renamedSpansTracker.IsDocumentChanged(documentId))
            {
                var document = await CurrentSolution.GetRequiredDocumentAsync(
                    documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // For the computeReplacementToken and computeReplacementNode functions, use 
                // the "updated" node to maintain any annotation removals from descendants.
                var newRoot = root.ReplaceSyntax(
                    nodes: annotationSet.GetAnnotatedNodes(root),
                    computeReplacementNode: (original, updated) => annotationSet.WithoutAnnotations(updated, [.. annotationSet.GetAnnotations(updated)]),
                    tokens: annotationSet.GetAnnotatedTokens(root),
                    computeReplacementToken: (original, updated) => annotationSet.WithoutAnnotations(updated, [.. annotationSet.GetAnnotations(updated)]),
                    trivia: [],
                    computeReplacementTrivia: null);

                intermediateSolution = await WithDocumentSyntaxRootAsync(intermediateSolution, documentId, newRoot, cancellationToken).ConfigureAwait(false);
            }
        }

        return intermediateSolution;
    }

    internal void RenameDocumentToMatchNewSymbol(Document document)
    {
        var extension = Path.GetExtension(document.Name);
        var newName = Path.ChangeExtension(ReplacementText, extension);

        // If possible, check that the new file name is unique to on disk files as well 
        // as solution items.
        IOUtilities.PerformIO(() =>
        {
            if (File.Exists(document.FilePath))
            {
                var directory = Directory.GetParent(document.FilePath)?.FullName;
                Contract.ThrowIfNull(directory);
                var newDocumentFilePath = Path.Combine(directory, newName);

                var versionNumber = 1;
                while (File.Exists(newDocumentFilePath))
                {
                    if (newName.Equals(document.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // If the document name is the same as the original, we know 
                        // it can be renamed to that because the old file on disk will
                        // be removed.
                        return;
                    }

                    var nameWithoutExtension = ReplacementText + $"_{versionNumber++}";
                    newName = Path.ChangeExtension(nameWithoutExtension, extension);
                    newDocumentFilePath = Path.Combine(directory, newName);
                }
            }
        });

        _renamedDocument = (document.Id, newName);
    }

    public int GetAdjustedTokenStartingPosition(int startingPosition, DocumentId documentId)
        => renamedSpansTracker.GetAdjustedPosition(startingPosition, documentId);

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
        var documentIds = renamedSpansTracker.DocumentIds.Concat(
            this.RelatedLocations.Select(l => l.DocumentId)).Distinct().ToImmutableArray();

        var relatedLocations = this.RelatedLocations.ToImmutableArray();

        var documentToModifiedSpansMap = renamedSpansTracker.GetDocumentToModifiedSpansMap();
        var documentToComplexifiedSpansMap = renamedSpansTracker.GetDocumentToComplexifiedSpansMap();
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

    /// <summary>
    /// Updates the syntax root of a document in the solution with a new syntax root.
    /// </summary>
    /// <remarks>
    /// This method specifically is used to handle source generated documents, when that option is on, which need some extra
    /// work before calling the normal WithDocumentSyntaxRoot method. If the option is not set, there should be no source generated
    /// documents, and the method will complete synchronously.
    /// </remarks>
    internal static async ValueTask<Solution> WithDocumentSyntaxRootAsync(Solution solution, DocumentId documentId, SyntaxNode newRoot, CancellationToken cancellationToken)
    {
        // In a source generated document, we have to ensure we've realized the "old" tree in the modified solution or WithDocumentSyntaxRoot
        // won't work. Performing a rename in a source generated document is opt-in, so we can assume that we only hit this condition in
        // scenarios that wanted it.
        if (documentId.IsSourceGenerated)
        {
            _ = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
        }

        return solution.WithDocumentSyntaxRoot(documentId, newRoot, PreservationMode.PreserveIdentity);
    }
}
