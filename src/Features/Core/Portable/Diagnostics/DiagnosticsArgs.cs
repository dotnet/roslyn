// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticsArgs : EventArgs
    {
        public object Id { get; }
        public Workspace Workspace { get; }
        public ProjectId ProjectId { get; }
        public DocumentId DocumentId { get; }

        public DiagnosticsArgs(object id, Workspace workspace, ProjectId projectId, DocumentId documentId)
        {
            this.Id = id;
            this.Workspace = workspace;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
        }
    }
}
