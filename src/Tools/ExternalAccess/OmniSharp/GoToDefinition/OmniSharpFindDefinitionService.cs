// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Navigation;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.GoToDefinition
{
    internal static class OmniSharpFindDefinitionService
    {
        internal static async Task<ImmutableArray<OmniSharpNavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IFindDefinitionService>();
            var result = await service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            var items = await result.NullToEmpty().SelectAsArrayAsync(
                async (original, solution, cancellationToken) =>
                {
                    var document = await original.Document.GetRequiredDocumentAsync(solution, cancellationToken).ConfigureAwait(false);
                    if (document is null)
                        return (OmniSharpNavigableItem?)null;

                    return new OmniSharpNavigableItem(original.DisplayTaggedParts, document, original.SourceSpan);
                },
                document.Project.Solution,
                cancellationToken).ConfigureAwait(false);
            return items.WhereNotNull();
        }
    }
}
