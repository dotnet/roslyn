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
    internal sealed class DefaultSymbolNavigationService : ISymbolNavigationService
    {
        public Task<bool> TryNavigateToSymbolAsync(ISymbol symbol, Project project, NavigationOptions options, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> TrySymbolNavigationNotifyAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<(string filePath, LinePosition linePosition)?> GetExternalNavigationSymbolLocationAsync(
            DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            return Task.FromResult<(string filePath, LinePosition linePosition)?>(null);
        }
    }
}
