// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation;

internal interface ISymbolNavigationService : IWorkspaceService
{
    /// <summary>
    /// Navigate to the first source location of a given symbol.
    /// </summary>
    /// <param name="project">A project context with which to generate source for symbol
    /// if it has no source locations</param>
    /// <param name="symbol">The symbol to navigate to</param>
    Task<INavigableLocation?> GetNavigableLocationAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

    /// <returns>True if the navigation was handled, indicating that the caller should not 
    /// perform the navigation.</returns>
    Task<bool> TrySymbolNavigationNotifyAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

    /// <summary>Returns the location file and position we would navigate to for the given <see cref="DefinitionItem"/>.</summary>
    /// <returns>Non-null if the navigation would be handled.</returns>
    Task<(string filePath, LinePosition linePosition)?> GetExternalNavigationSymbolLocationAsync(
        DefinitionItem definitionItem, CancellationToken cancellationToken);
}
