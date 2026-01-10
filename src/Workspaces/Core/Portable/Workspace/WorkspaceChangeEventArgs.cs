// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// The <see cref="EventArgs"/> describing any kind of workspace change.
/// </summary>
/// <remarks>
/// When linked files are edited, one document change event is fired per linked file. All of
/// these events contain the same <see cref="OldSolution"/>, and they all contain the same
/// <see cref="NewSolution"/>. This is so that we can trigger document change events on all
/// affected documents without reporting intermediate states in which the linked file contents
/// do not match.
/// </remarks>
public class WorkspaceChangeEventArgs : EventArgs
{
    public WorkspaceChangeKind Kind { get; }

    /// <remarks>
    /// If linked documents are being changed, there may be multiple events with the same <see cref="OldSolution"/>
    /// and <see cref="NewSolution"/>.  Note that the workspace starts with its solution set to an empty solution.
    /// <see cref="WorkspaceChangeKind.SolutionAdded"/> replaces the previous solution, which might be the empty
    /// one.
    /// </remarks>
    public Solution OldSolution { get; }

    /// <remarks>
    /// If linked documents are being changed, there may be multiple events with the same <see cref="OldSolution"/>
    /// and <see cref="NewSolution"/>. Note <see cref="WorkspaceChangeKind.SolutionRemoved"/> replaces the previous
    /// solution with the empty one.
    /// </remarks>
    public Solution NewSolution { get; }

    /// <summary>
    /// The id of the affected <see cref="Project"/>.  Can be <see langword="null"/> if this is an change unrelated
    /// to a project (for example <see cref="WorkspaceChangeKind.SolutionReloaded"/>.  Should be non-<see
    /// langword="null"/> for:
    /// <list type="bullet">
    /// <item><description><see cref="WorkspaceChangeKind.ProjectAdded"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.ProjectChanged"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.ProjectReloaded"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.ProjectRemoved"/></description></item>
    /// </list>
    /// </summary>
    public ProjectId? ProjectId { get; }

    /// <summary>
    /// The id of the affected <see cref="Document"/>.  Can be <see langword="null"/> if this is an change unrelated
    /// to a document (for example <see cref="WorkspaceChangeKind.ProjectAdded"/>. Should be non-<see
    /// langword="null"/> for:
    /// <list type="bullet">
    /// <item><description><see cref="WorkspaceChangeKind.DocumentAdded"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.DocumentChanged"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.DocumentInfoChanged"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.DocumentReloaded"/></description></item>
    /// <item><description><see cref="WorkspaceChangeKind.DocumentRemoved"/></description></item>
    /// </list>
    /// </summary>
    public DocumentId? DocumentId { get; }

    public WorkspaceChangeEventArgs(WorkspaceChangeKind kind, Solution oldSolution, Solution newSolution, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        if (!kind.IsValid())
            throw new ArgumentOutOfRangeException(nameof(kind));

        this.Kind = kind;
        this.OldSolution = oldSolution ?? throw new ArgumentNullException(nameof(oldSolution));
        this.NewSolution = newSolution ?? throw new ArgumentNullException(nameof(newSolution));
        this.ProjectId = projectId;
        this.DocumentId = documentId;
    }
}
