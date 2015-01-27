// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticsUpdatedArgs : EventArgs
    {
        public object Id { get; private set; }
        public Workspace Workspace { get; private set; }
        public Solution Solution { get; private set; }
        public ProjectId ProjectId { get; private set; }
        public DocumentId DocumentId { get; private set; }
        public ImmutableArray<DiagnosticData> Diagnostics { get; private set; }

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
