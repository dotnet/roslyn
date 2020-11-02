// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService), ServiceLayer.Default)]
    [Shared]
    internal sealed class DefaultWorkspaceTelemetryService : IWorkspaceTelemetryService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultWorkspaceTelemetryService()
        {
        }

        public bool HasActiveSession => false;

        public void RegisterUnexpectedExceptionLogger(TraceSource logger)
        {
        }

        public void ReportApiUsage(HashSet<ISymbol> symbols, Guid solutionSessionId, Guid projectGuid)
        {
        }

        public string? SerializeCurrentSessionSettings()
        {
            return null;
        }

        public void UnregisterUnexpectedExceptionLogger(TraceSource logger)
        {
        }
    }
}
