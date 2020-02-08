// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
