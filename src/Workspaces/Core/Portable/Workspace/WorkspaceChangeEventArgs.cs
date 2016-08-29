// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public class WorkspaceChangeEventArgs : EventArgs
    {
        public WorkspaceChangeKind Kind { get; }
        public Solution OldSolution { get; }
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
