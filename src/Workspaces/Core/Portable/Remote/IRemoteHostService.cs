// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteHostService
    {
        string Connect(string host, string serializedSession, CancellationToken cancellationToken);
        Task SynchronizePrimaryWorkspaceAsync(Checksum checksum);
        Task SynchronizeGlobalAssetsAsync(Checksum[] checksums);

        void RegisterPrimarySolutionId(SolutionId solutionId);
        void UnregisterPrimarySolutionId(SolutionId solutionId, bool synchronousShutdown);

        void UpdateSolutionIdStorageLocation(SolutionId solutionId, string storageLocation);
    }
}
