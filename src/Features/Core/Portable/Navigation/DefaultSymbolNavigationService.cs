// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation;

internal sealed class DefaultSymbolNavigationService : ISymbolNavigationService
{
    public async Task<INavigableLocation?> GetNavigableLocationAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
        => null;

    public async Task<bool> TrySymbolNavigationNotifyAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
        => false;

    public async Task<(string filePath, LinePosition linePosition)?> GetExternalNavigationSymbolLocationAsync(DefinitionItem definitionItem, CancellationToken cancellationToken)
        => null;
}
