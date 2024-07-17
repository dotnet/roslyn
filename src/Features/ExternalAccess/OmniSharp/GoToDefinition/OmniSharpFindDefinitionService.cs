// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Navigation;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.GoToDefinition;

internal static class OmniSharpFindDefinitionService
{
    internal static async Task<ImmutableArray<OmniSharpNavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var service = document.GetLanguageService<INavigableItemsService>();
        if (service is null)
            return ImmutableArray<OmniSharpNavigableItem>.Empty;

        var result = await service.GetNavigableItemsAsync(document, position, cancellationToken).ConfigureAwait(false);
        return await result.NullToEmpty().SelectAsArrayAsync(
            selector: async (original, solution, cancellationToken) => new OmniSharpNavigableItem(original.DisplayTaggedParts, await original.Document.GetRequiredDocumentAsync(solution, cancellationToken).ConfigureAwait(false), original.SourceSpan),
            arg: document.Project.Solution,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
