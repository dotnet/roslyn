// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Engine use this to set or update solution for given session (connection)
    /// </summary>
    internal interface ISolutionController
    {
        Task<Solution> GetSolutionAsync(Checksum solutionChecksum, bool fromPrimaryBranch, int workspaceVersion, CancellationToken cancellationToken);

        Task UpdatePrimaryWorkspaceAsync(Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken);
    }
}
