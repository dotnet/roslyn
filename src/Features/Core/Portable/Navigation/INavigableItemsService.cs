// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Navigation;

/// <summary>
/// Service used for features that want to find all the locations to potentially navigate to for a symbol at a
/// particular location, with enough information provided to display those locations in a rich fashion. Differs from
/// <see cref="IDefinitionLocationService"/> in that this can show a rich display of the items, not just navigate to
/// them.
/// </summary>
internal interface INavigableItemsService : ILanguageService
{
    /// <summary>
    /// Finds the definitions for the symbol at the specific position in the document.
    /// </summary>
    Task<ImmutableArray<INavigableItem>> GetNavigableItemsAsync(Document document, int position, CancellationToken cancellationToken);
}
