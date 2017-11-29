// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IDiagnosticService
    {
        /// <summary>
        /// Event to get notified as new diagnostics are discovered by <see cref="IDiagnosticUpdateSource"/>
        /// This event is host wide meaning it will be raised for host workspace, misc workspace, preivew workspace and etc. workspace is significant since same project/documentId can
        /// exist in multiple workspaces.
        /// </summary>
        event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        /// <summary>
        /// Get cached diagnostics stored in <see cref="IDiagnosticUpdateSource"/>. diagnostics returned by it can be staled ones. not all <see cref="IDiagnosticUpdateSource"/> can provide
        /// up to date diagnostics (ex, diagnostics from build). if one wants up to date diagnostics, it should use <see cref="IDiagnosticUpdateSource"/> directly such as <see cref="IDiagnosticAnalyzerService"/> that
        /// provides APIs such as <see cref="IDiagnosticAnalyzerService.GetDiagnosticsAsync(Solution, ProjectId, DocumentId, bool, CancellationToken)"/> which accepts <see cref="Solution"/> snapshot to 
        /// specify which specific snapshot it wants diagnostics from.
        /// </summary>
        IEnumerable<DiagnosticData> GetCachedDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get current UpdatedEventArgs stored in IDiagnosticUpdateSource
        /// </summary>
        IEnumerable<UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs(Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken);

        /// <summary>
        /// Subscribe to document specific changes when one doesn't need to listen to host wide changes like error list.
        /// </summary>
        IDisposable Subscribe(Workspace workspace, DocumentId documentId, Action<DiagnosticsUpdatedArgs> action);
    }
}
