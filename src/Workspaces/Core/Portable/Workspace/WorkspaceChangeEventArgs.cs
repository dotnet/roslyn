// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// If linked documents are being changed, there may be multiple events with the same
        /// <see cref="OldSolution"/> and <see cref="NewSolution"/>.
        /// </remarks>
        public Solution OldSolution { get; }

        /// <remarks>
        /// If linked documents are being changed, there may be multiple events with the same
        /// <see cref="OldSolution"/> and <see cref="NewSolution"/>.
        /// </remarks>
        public Solution NewSolution { get; }

        public ProjectId ProjectId { get; }
        public DocumentId DocumentId { get; }

        public WorkspaceChangeEventArgs(WorkspaceChangeKind kind, Solution oldSolution, Solution newSolution, ProjectId projectId = null, DocumentId documentId = null)
        {
            this.Kind = kind;
            this.OldSolution = oldSolution;
            this.NewSolution = newSolution;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
        }
    }
}
