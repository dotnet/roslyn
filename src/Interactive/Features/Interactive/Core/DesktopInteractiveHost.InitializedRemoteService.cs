// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class DesktopInteractiveHost
    {
        private struct InitializedRemoteService
        {
            public readonly RemoteService ServiceOpt;
            public readonly InteractiveExecutionResult InitializationResult;

            public InitializedRemoteService(RemoteService service, InteractiveExecutionResult initializationResult)
            {
                ServiceOpt = service;
                InitializationResult = initializationResult;
            }
        }
    }
}
