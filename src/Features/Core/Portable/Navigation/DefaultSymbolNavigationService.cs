// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal class DefaultSymbolNavigationService : ISymbolNavigationService
    {
        public bool TryNavigateToSymbol(ISymbol symbol, Project project, OptionSet? options = null, CancellationToken cancellationToken = default)
            => false;

        public Task<bool> TrySymbolNavigationNotifyAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<(string filePath, LinePosition linePosition)?> GetExternalNavigationSymbolLocationAsync(
            DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            return Task.FromResult<(string filePath, LinePosition linePosition)?>(null);
        }
    }
}
