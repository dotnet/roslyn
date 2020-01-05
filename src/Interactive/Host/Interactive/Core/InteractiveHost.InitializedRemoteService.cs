// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        private struct InitializedRemoteService
        {
            public readonly RemoteService ServiceOpt;
            public readonly RemoteExecutionResult InitializationResult;

            public InitializedRemoteService(RemoteService service, RemoteExecutionResult initializationResult)
            {
                ServiceOpt = service;
                InitializationResult = initializationResult;
            }
        }
    }
}
