// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolFinder
    {
        public async Task<SerializableSymbolAndProjectId[]> FindSolutionSourceDeclarationsAsync(
            string name, bool ignoreCase, SymbolFilter filter)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var symbols = await SymbolFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                solution, name, ignoreCase, filter, CancellationToken).ConfigureAwait(false);

            return symbols.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }

        public async Task<SerializableSymbolAndProjectId[]> FindProjectSourceDeclarationsAsync(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter filter)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var project = solution.GetProject(projectId);

            var symbols = await SymbolFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, name, ignoreCase, filter, CancellationToken).ConfigureAwait(false);

            return symbols.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }
    }
}