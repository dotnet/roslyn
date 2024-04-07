// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

/// <summary>
/// A little helper type to hold onto the <see cref="Solution"/> being updated in a batch, which also
/// keeps track of the right <see cref="CodeAnalysis.WorkspaceChangeKind"/> to raise when we are done.
/// </summary>
internal sealed class SolutionChangeAccumulator(Solution startingSolution)
{
    /// <summary>
    /// The kind that encompasses all the changes we've made. It's null if no changes have been made,
    /// and <see cref="WorkspaceChangeKind.ProjectChanged"/> or
    /// <see cref="WorkspaceChangeKind.SolutionChanged"/> if we can't give a more precise type.
    /// </summary>
    private WorkspaceChangeKind? _workspaceChangeKind;
    private readonly List<DocumentId> _documentIdsRemoved = [];

    public Solution Solution { get; private set; } = startingSolution;
    public IEnumerable<DocumentId> DocumentIdsRemoved => _documentIdsRemoved;

    public bool HasChange => _workspaceChangeKind.HasValue;
    public WorkspaceChangeKind WorkspaceChangeKind => _workspaceChangeKind!.Value;

    public ProjectId? WorkspaceChangeProjectId { get; private set; }
    public DocumentId? WorkspaceChangeDocumentId { get; private set; }

    public void UpdateSolutionForDocumentAction(Solution newSolution, WorkspaceChangeKind changeKind, IEnumerable<DocumentId> documentIds)
    {
        // If the newSolution is the same as the current solution, there's nothing to actually do
        if (Solution == newSolution)
        {
            return;
        }

        Solution = newSolution;

        foreach (var documentId in documentIds)
        {
            // If we don't previously have change, this is our new change
            if (!_workspaceChangeKind.HasValue)
            {
                _workspaceChangeKind = changeKind;
                WorkspaceChangeProjectId = documentId.ProjectId;
                WorkspaceChangeDocumentId = documentId;
            }
            else
            {
                // We do have a new change. At this point, the change is spanning multiple documents or projects we
                // will coalesce accordingly
                if (documentId.ProjectId == WorkspaceChangeProjectId)
                {
                    // It's the same project, at least, so project change it is
                    _workspaceChangeKind = WorkspaceChangeKind.ProjectChanged;
                    WorkspaceChangeDocumentId = null;
                }
                else
                {
                    // Multiple projects have changed, so it's a generic solution change. At this point
                    // we can bail from the loop, because this is already our most general case.
                    _workspaceChangeKind = WorkspaceChangeKind.SolutionChanged;
                    WorkspaceChangeProjectId = null;
                    WorkspaceChangeDocumentId = null;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// The same as <see cref="UpdateSolutionForDocumentAction(Solution, WorkspaceChangeKind, IEnumerable{DocumentId})" /> but also records
    /// the removed documents into <see cref="DocumentIdsRemoved"/>.
    /// </summary>
    public void UpdateSolutionForRemovedDocumentAction(Solution solution, WorkspaceChangeKind removeDocumentChangeKind, IEnumerable<DocumentId> documentIdsRemoved)
    {
        UpdateSolutionForDocumentAction(solution, removeDocumentChangeKind, documentIdsRemoved);

        _documentIdsRemoved.AddRange(documentIdsRemoved);
    }

    /// <summary>
    /// Should be called to update the solution if there isn't a specific document change kind that should be
    /// given to <see cref="UpdateSolutionForDocumentAction"/>
    /// </summary>
    public void UpdateSolutionForProjectAction(ProjectId projectId, Solution newSolution)
    {
        // If the newSolution is the same as the current solution, there's nothing to actually do
        if (Solution == newSolution)
        {
            return;
        }

        Solution = newSolution;

        // Since we're changing a project, we definitely have no DocumentId anymore
        WorkspaceChangeDocumentId = null;

        if (!_workspaceChangeKind.HasValue || WorkspaceChangeProjectId == projectId)
        {
            // We can count this as a generic project change
            _workspaceChangeKind = WorkspaceChangeKind.ProjectChanged;
            WorkspaceChangeProjectId = projectId;
        }
        else
        {
            _workspaceChangeKind = WorkspaceChangeKind.SolutionChanged;
            WorkspaceChangeProjectId = null;
        }
    }
}
