// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticsUpdatedArgs : UpdatedEventArgs
    {
        public Solution Solution { get; }
        public ImmutableArray<DiagnosticData> Diagnostics { get; }

        public DiagnosticsUpdatedArgs(
            object id, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics) :
                base(id, workspace, projectId, documentId)
        {
            this.Solution = solution;
            this.Diagnostics = diagnostics;
        }
    }
}
