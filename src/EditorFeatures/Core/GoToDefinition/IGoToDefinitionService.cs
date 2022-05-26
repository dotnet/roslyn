// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.GoToDefinition;

internal interface IGoToDefinitionService : ILanguageService
{
    /// <inheritdoc cref="CodeAnalysis.GoToDefinition.IFindDefinitionService.FindDefinitionsAsync(Document, int, CancellationToken)"/>
    // Keep changes to this method in sync with CodeAnalysis.GoToDefinition.IFindDefinitionService
    // Obsoletion is tracked with https://github.com/dotnet/roslyn/issues/50391
    Task<IEnumerable<INavigableItem>?> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the definitions for the symbol at the specific position in the document and then 
    /// navigates to them.
    /// </summary>
    /// <returns>True if navigating to the definition of the symbol at the provided position succeeds.  False, otherwise.</returns>
    bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken);
}
