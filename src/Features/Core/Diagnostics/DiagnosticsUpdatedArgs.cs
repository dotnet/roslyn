// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticsUpdatedArgs : EventArgs
    {
        public object Id { get; }
        public Workspace Workspace { get; }
        public Solution Solution { get; }
        public ProjectId ProjectId { get; }
        public DocumentId DocumentId { get; }
        public ImmutableArray<DiagnosticData> Diagnostics { get; }

        public DiagnosticsUpdatedArgs(
            object id, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
        {
            this.Id = id;
            this.Workspace = workspace;
            this.Solution = solution;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
            this.Diagnostics = diagnostics;
        }
    }
}
