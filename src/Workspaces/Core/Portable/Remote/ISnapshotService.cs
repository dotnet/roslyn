// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface ISnapshotService
    {
        Task SynchronizePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken);
        Task SynchronizeGlobalAssetsAsync(Checksum[] checksums, CancellationToken cancellationToken);
    }
}
