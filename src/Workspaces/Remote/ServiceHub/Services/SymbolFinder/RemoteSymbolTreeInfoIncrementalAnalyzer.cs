// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSymbolTreeInfoIncrementalAnalyzer : BrokeredServiceBase, IRemoteSymbolTreeInfoIncrementalAnalyzer
    {
        internal sealed class Factory : FactoryBase<IRemoteSymbolTreeInfoIncrementalAnalyzer>
        {
            protected override IRemoteSymbolTreeInfoIncrementalAnalyzer CreateService(in ServiceConstructionArguments arguments)
                => new RemoteSymbolTreeInfoIncrementalAnalyzer(arguments);
        }

        public RemoteSymbolTreeInfoIncrementalAnalyzer(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask AnalyzeDocumentAsync(
            Checksum solutionChecksum,
            DocumentId documentId,
            bool isMethodBodyEdit,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                solutionChecksum,
                async solution =>
                {
                    var cacheService = solution.Services.GetRequiredService<SymbolTreeInfoCacheService>();
                    await cacheService.AnalyzeDocumentAsync(solution.GetRequiredDocument(documentId), isMethodBodyEdit, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken);
        }

        public ValueTask AnalyzeProjectAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                solutionChecksum,
                async solution =>
                {
                    var cacheService = solution.Services.GetRequiredService<SymbolTreeInfoCacheService>();
                    await cacheService.AnalyzeProjectAsync(solution.GetRequiredProject(projectId), cancellationToken).ConfigureAwait(false);
                },
                cancellationToken);
        }

        public ValueTask RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                cancellationToken =>
                {
                    var cacheService = GetWorkspaceServices().GetRequiredService<SymbolTreeInfoCacheService>();
                    cacheService.RemoveProject(projectId);
                    return default;
                },
                cancellationToken);
        }
    }
}
