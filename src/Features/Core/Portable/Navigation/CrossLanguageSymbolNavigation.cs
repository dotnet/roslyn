// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Navigation;

internal static class CrossLanguageSymbolNavigation
{
    /// <summary>
    /// The span of the source definition another .Net language, for example F#, has for the metadata symbol
    /// <paramref name="definitionItem"/> was created for.  <see langword="null"/> for an item created for a symbol in
    /// source, or when no <see cref="ICrossLanguageSymbolNavigationService"/> defines the symbol.
    /// </summary>
    public static async Task<DocumentSpan?> TryGetDefinitionSpanAsync(
        Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
    {
        var (_, symbol) = await definitionItem.TryResolveMetadataSymbolAsync(solution, cancellationToken).ConfigureAwait(false);
        var documentationCommentId = symbol?.GetDocumentationCommentId();
        var assemblyName = symbol?.ContainingAssembly?.Identity.Name;
        if (documentationCommentId is null || assemblyName is null)
            return null;

        foreach (var lazyService in solution.Services.ExportProvider.GetExports<ICrossLanguageSymbolNavigationService>())
        {
            var span = await lazyService.Value.TryGetDefinitionSpanAsync(
                assemblyName, documentationCommentId, cancellationToken).ConfigureAwait(false);
            if (span != null)
                return span;
        }

        return null;
    }
}
