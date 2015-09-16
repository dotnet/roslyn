// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticUpdateSource))]
    internal partial class DiagnosticAnalyzerService : IDiagnosticUpdateSource
    {
        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        internal void RaiseDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs state)
        {
            var handler = this.DiagnosticsUpdated;
            if (handler != null)
            {
                handler(sender, state);
            }
        }

        bool IDiagnosticUpdateSource.SupportGetDiagnostics { get { return true; } }

        ImmutableArray<DiagnosticData> IDiagnosticUpdateSource.GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                return GetSpecificCachedDiagnosticsAsync(workspace, id, includeSuppressedDiagnostics, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            return GetCachedDiagnosticsAsync(workspace, projectId, documentId, includeSuppressedDiagnostics, cancellationToken).WaitAndGetResult(cancellationToken);
        }
    }
}
