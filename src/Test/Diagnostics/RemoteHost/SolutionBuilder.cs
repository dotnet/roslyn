using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Assets;

namespace Roslyn.Hosting.Diagnostics.RemoteHost
{
    internal class SolutionBuilder
    {
        private Tuple<Solution, Dictionary<(string, string), ProjectOrDocumentId>> _cachedData = null;

        public async Task<Solution> GetSolutionOrDefaultAsync(string workspaceId, Solution solution, CancellationToken cancellationToken)
        {
            return (await GetSolutionAndIdMapAsync(workspaceId, cancellationToken).ConfigureAwait(false)).solution ?? solution;
        }

        public Task<(Solution solution, IReadOnlyDictionary<(string, string), ProjectOrDocumentId> idMap)> GetSolutionAndIdMapAsync(string workspaceId, CancellationToken cancellationToken)
        {
            return Task.FromResult<(Solution solution, IReadOnlyDictionary<(string, string), ProjectOrDocumentId> idMap)>((_cachedData.Item1, _cachedData.Item2));
        }
    }
}
