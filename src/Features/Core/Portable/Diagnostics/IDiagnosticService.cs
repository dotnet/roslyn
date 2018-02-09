// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Common;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IDiagnosticService
    {
        /// <summary>
        /// Event to get notified as new diagnostics are discovered by IDiagnosticUpdateSource
        /// 
        /// These events are serialized to preserve event ordering using <see cref="EventMap"/> and <see cref="SimpleTaskQueue"/>
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
