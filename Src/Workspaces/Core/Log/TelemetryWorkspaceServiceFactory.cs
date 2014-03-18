// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    [ExportWorkspaceServiceFactory(typeof(ITelemetryService), WorkspaceKind.Any)]
    internal class TelemetryWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new EmptyTelemetryLogger();
        }

        private class EmptyTelemetryLogger : ITelemetryService
        {
            void ITelemetryService.EndCurrentSession()
            {
            }

            void ITelemetryService.LogLightBulbSession(Telemetry.LightBulbSessionInfo lightBulbSession)
            {
            }

            void ITelemetryService.LogRenameSession(Telemetry.RenameSessionInfo renameSession)
            {
            }
        }
    }
}
