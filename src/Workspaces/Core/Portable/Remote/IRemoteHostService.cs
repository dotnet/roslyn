// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteHostService
    {
        string Connect(string host, string serializedSession);
        Task SynchronizePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken);
        Task SynchronizeGlobalAssetsAsync(Checksum[] checksums, CancellationToken cancellationToken);

        void RegisterPrimarySolutionId(SolutionId solutionId, string storageLocation, CancellationToken cancellationToken);
        void UnregisterPrimarySolutionId(SolutionId solutionId, bool synchronousShutdown, CancellationToken cancellationToken);

        void OnGlobalOperationStarted(string operation, CancellationToken cancellationToken);
        void OnGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled, CancellationToken cancellationToken);
    }
}
