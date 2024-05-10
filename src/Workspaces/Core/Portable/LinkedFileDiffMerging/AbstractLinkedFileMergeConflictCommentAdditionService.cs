// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal abstract class AbstractLinkedFileMergeConflictCommentAdditionService : IMergeConflictHandler, ILanguageService, ILinkedFileMergeConflictCommentAdditionService
{
    internal abstract string GetConflictCommentText(string header, string beforeString, string afterString);

    public ImmutableArray<TextChange> CreateEdits(SourceText originalSourceText, ArrayBuilder<UnmergedDocumentChanges> unmergedChanges)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var commentChanges);

        foreach (var documentWithChanges in unmergedChanges)
        {
            var partitionedChanges = PartitionChangesForDocument(documentWithChanges.UnmergedChanges, originalSourceText);
            var comments = GetCommentChangesForDocument(partitionedChanges, documentWithChanges.ProjectName, originalSourceText);

            commentChanges.AddRange(comments);
        }

        return commentChanges.ToImmutableAndClear();
    }

    private static List<List<TextChange>> PartitionChangesForDocument(IEnumerable<TextChange> changes, SourceText originalSourceText)
    {
        var partitionedChanges = new List<List<TextChange>>();
        var currentPartition = new List<TextChange>
        {
            changes.First()
        };
        var currentPartitionEndLine = originalSourceText.Lines.GetLineFromPosition(changes.First().Span.End);

        foreach (var change in changes.Skip(1))
        {
            // If changes are on adjacent lines, consider them part of the same change.
            var changeStartLine = originalSourceText.Lines.GetLineFromPosition(change.Span.Start);
            if (changeStartLine.LineNumber >= currentPartitionEndLine.LineNumber + 2)
            {
                partitionedChanges.Add(currentPartition);
                currentPartition = [];
            }

            currentPartition.Add(change);
            currentPartitionEndLine = originalSourceText.Lines.GetLineFromPosition(change.Span.End);
        }

        if (currentPartition.Any())
        {
            partitionedChanges.Add(currentPartition);
        }

        return partitionedChanges;
    }

    private List<TextChange> GetCommentChangesForDocument(IEnumerable<IEnumerable<TextChange>> partitionedChanges, string projectName, SourceText oldDocumentText)
    {
        var commentChanges = new List<TextChange>();

        foreach (var changePartition in partitionedChanges)
        {
            var startPosition = changePartition.First().Span.Start;
            var endPosition = changePartition.Last().Span.End;

            var startLineStartPosition = oldDocumentText.Lines.GetLineFromPosition(startPosition).Start;
            var endLineEndPosition = oldDocumentText.Lines.GetLineFromPosition(endPosition).End;

            var oldText = oldDocumentText.GetSubText(TextSpan.FromBounds(startLineStartPosition, endLineEndPosition));
            var adjustedChanges = changePartition.Select(c => new TextChange(TextSpan.FromBounds(c.Span.Start - startLineStartPosition, c.Span.End - startLineStartPosition), c.NewText));
            var newText = oldText.WithChanges(adjustedChanges);

            var warningText = GetConflictCommentText(
                string.Format(WorkspacesResources.Unmerged_change_from_project_0, projectName),
                TrimBlankLines(oldText),
                TrimBlankLines(newText));

            if (warningText != null)
            {
                commentChanges.Add(new TextChange(TextSpan.FromBounds(startLineStartPosition, startLineStartPosition), warningText));
            }
        }

        return commentChanges;
    }

    private static string TrimBlankLines(SourceText text)
    {
        int startLine, endLine;
        for (startLine = 0; startLine < text.Lines.Count; startLine++)
        {
            if (!text.Lines[startLine].IsEmptyOrWhitespace())
            {
                break;
            }
        }

        for (endLine = text.Lines.Count - 1; endLine > startLine; endLine--)
        {
            if (!text.Lines[endLine].IsEmptyOrWhitespace())
            {
                break;
            }
        }

        return startLine <= endLine
            ? text.GetSubText(TextSpan.FromBounds(text.Lines[startLine].Start, text.Lines[endLine].End)).ToString()
            : null;
    }
}
