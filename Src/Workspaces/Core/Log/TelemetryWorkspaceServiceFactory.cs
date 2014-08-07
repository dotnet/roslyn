// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log.Telemetry;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    [ExportWorkspaceServiceFactory(typeof(ITelemetryService), ServiceLayer.Default)]
    internal class TelemetryWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new EmptyTelemetryLogger();
        }

        private class EmptyTelemetryLogger : ITelemetryService
        {
            void ITelemetryService.EndCurrentSession()
            {
            }

            void ITelemetryService.LogRenameSession(Telemetry.RenameSessionInfo renameSession)
            {
            }
        }
    }
}
