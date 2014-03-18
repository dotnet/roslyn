// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log.Telemetry;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Provides a way to log qualitative usage data
    /// </summary>
    internal interface ITelemetryService : IWorkspaceService
    {
        void EndCurrentSession();
        void LogRenameSession(RenameSessionInfo renameSession);
        void LogLightBulbSession(LightBulbSessionInfo lightBulbSession);
    }
}
