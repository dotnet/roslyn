﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
