// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal class DebugWindowResult : ParsedLine
    {
        private readonly string _symbolData;

        public DebugWindowResult(string symbolData)
        {
            _symbolData = symbolData;
        }

        internal override async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            var (methodName, methodArgs) = GetMethodSignatureParts(_symbolData);

            foreach (var project in solution.Projects)
            {
                var foundSymbols = await FindSymbols.DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                    project,
                    methodName,
                    SymbolFilter.Member,
                    cancellationToken).ConfigureAwait(false);

                var foundSymbol = foundSymbols.Length == 1
                    ? foundSymbols[0]
                    : null;

                if (foundSymbol is not null)
                {
                    return foundSymbol;
                }
            }

            return null;
        }
    }
}
