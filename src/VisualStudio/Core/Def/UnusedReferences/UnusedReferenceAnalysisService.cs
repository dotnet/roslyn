// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets;

namespace Microsoft.CodeAnalysis.UnusedReferences;

[ExportWorkspaceService(typeof(IUnusedReferenceAnalysisService)), Shared]
internal partial class UnusedReferenceAnalysisService : IUnusedReferenceAnalysisService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public UnusedReferenceAnalysisService()
    {
    }

    public async Task<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(Solution solution, string projectFilePath, string projectAssetsFilePath, ImmutableArray<ReferenceInfo> projectReferences, CancellationToken cancellationToken)
    {
        using var logger = Logger.LogBlock(FunctionId.UnusedReferences_GetUnusedReferences, message: null, cancellationToken, LogLevel.Information);
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var result = await client.TryInvokeAsync<IRemoteUnusedReferenceAnalysisService, ImmutableArray<ReferenceInfo>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.GetUnusedReferencesAsync(solutionInfo, projectFilePath, projectAssetsFilePath, projectReferences, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            return result.Value;
        }

        // Read specified references with dependency information from the project assets file.
        var references = await ProjectAssetsFileReader.ReadReferencesAsync(projectReferences, projectAssetsFilePath).ConfigureAwait(false);

        // Determine unused references
        var unusedReferences = await UnusedReferencesRemover.GetUnusedReferencesAsync(solution, projectFilePath, references, cancellationToken).ConfigureAwait(false);

        // Remove dependency information before returning.
        return unusedReferences.SelectAsArray(reference => reference.WithDependencies(null));
    }
}
