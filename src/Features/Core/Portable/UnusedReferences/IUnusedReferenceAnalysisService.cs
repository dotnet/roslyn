// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal interface IUnusedReferenceAnalysisService : IWorkspaceService
    {
        Task<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(
            Solution solution,
            string projectFilePath,
            string projectAssetsFilePath,
            ImmutableArray<ReferenceInfo> projectReferences,
            CancellationToken cancellationToken);
    }

    internal interface IRemoteUnusedReferenceAnalysisService
    {
        ValueTask<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(
            Checksum solutionChecksum,
            string projectFilePath,
            string projectAssetsFilePath,
            ImmutableArray<ReferenceInfo> projectReferences,
            CancellationToken cancellationToken);
    }
}
