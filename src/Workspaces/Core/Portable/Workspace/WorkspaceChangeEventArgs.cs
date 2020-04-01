// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis
{
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
        /// and <see cref="NewSolution"/>.  Will never be <see langword="null"/>, even for a <see
        /// cref="WorkspaceChangeKind.SolutionAdded"/> notification.
        /// </remarks>
        public Solution OldSolution { get; }

        /// <remarks>
        /// If linked documents are being changed, there may be multiple events with the same <see cref="OldSolution"/>
        /// and <see cref="NewSolution"/>. Will never be <see langword="null"/>, even for a <see
        /// cref="WorkspaceChangeKind.SolutionRemoved"/> notification.
        /// </remarks>
        public Solution NewSolution { get; }

        /// <summary>
        /// The id of the affected <see cref="Project"/>.  Can be <see langword="null"/> if this is an change unrelated
        /// to a project (for example <see cref="WorkspaceChangeKind.SolutionReloaded"/>.  Should be non-<see
        /// langword="null"/> for:
        /// <list type="bullet">
        /// <item><see cref="WorkspaceChangeKind.ProjectAdded"/></item>
        /// <item><see cref="WorkspaceChangeKind.ProjectChanged"/></item>
        /// <item><see cref="WorkspaceChangeKind.ProjectReloaded"/></item>
        /// <item><see cref="WorkspaceChangeKind.ProjectRemoved"/></item>
        /// </list>
        /// </summary>
        public ProjectId? ProjectId { get; }

        /// <summary>
        /// The id of the affected <see cref="Document"/>.  Can be <see langword="null"/> if this is an change unrelated
        /// to a document (for example <see cref="WorkspaceChangeKind.ProjectAdded"/>. Should be non-<see
        /// langword="null"/> for:
        /// <list type="bullet">
        /// <item><see cref="WorkspaceChangeKind.DocumentAdded"/></item>
        /// <item><see cref="WorkspaceChangeKind.DocumentChanged"/></item>
        /// <item><see cref="WorkspaceChangeKind.DocumentInfoChanged"/></item>
        /// <item><see cref="WorkspaceChangeKind.DocumentReloaded"/></item>
        /// <item><see cref="WorkspaceChangeKind.DocumentRemoved"/></item>
        /// </list>
        /// </summary>
        public DocumentId? DocumentId { get; }

        public WorkspaceChangeEventArgs(WorkspaceChangeKind kind, Solution oldSolution, Solution newSolution, ProjectId? projectId = null, DocumentId? documentId = null)
        {
            this.Kind = kind;
            this.OldSolution = oldSolution;
            this.NewSolution = newSolution;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
        }
    }
}
