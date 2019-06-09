// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Aggregates events from various diagnostic sources.
    /// </summary>
    internal interface IDiagnosticService
    {
        /// <summary>
        /// Event to get notified as new diagnostics are discovered by IDiagnosticUpdateSource
        /// 
        /// Notifications for this event are serialized to preserve order.
        /// However, individual event notifications may occur on any thread.
        /// </summary>
        event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        /// <summary>
        /// Get current diagnostics stored in IDiagnosticUpdateSource
        /// </summary>
        IEnumerable<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get current UpdatedEventArgs stored in IDiagnosticUpdateSource
        /// </summary>
        IEnumerable<UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs(Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken);
    }
}
