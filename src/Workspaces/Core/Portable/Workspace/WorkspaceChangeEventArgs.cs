// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public class WorkspaceChangeEventArgs : EventArgs
    {
        public WorkspaceChangeKind Kind { get; private set; }
        public Solution OldSolution { get; private set; }
        public Solution NewSolution { get; private set; }
        public ProjectId ProjectId { get; private set; }
        public DocumentId DocumentId { get; private set; }

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