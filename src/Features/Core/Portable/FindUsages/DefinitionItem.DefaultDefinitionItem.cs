// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindUsages;

internal abstract partial class DefinitionItem
{
    /// <summary>
    /// Implementation of a <see cref="DefinitionItem"/> that sits on top of a 
    /// <see cref="DocumentSpan"/>.
    /// </summary>
    // internal for testing purposes.
    internal sealed class DefaultDefinitionItem(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<TaggedText> nameDisplayParts,
        ImmutableArray<DocumentSpan> sourceSpans,
        ImmutableArray<ClassifiedSpansAndHighlightSpan?> classifiedSpans,
        ImmutableArray<AssemblyLocation> metadataLocations,
        ImmutableDictionary<string, string>? properties,
        ImmutableArray<(string key, string value)> displayableProperties,
        bool displayIfNoReferences) : DefinitionItem(
            tags, displayParts, nameDisplayParts,
            sourceSpans, classifiedSpans, metadataLocations, properties, displayableProperties, displayIfNoReferences)
    {
        internal sealed override bool IsExternal => false;

        public override async Task<INavigableLocation?> GetNavigableLocationAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            if (Properties.ContainsKey(NonNavigable))
                return null;

            if (Properties.ContainsKey(MetadataSymbolKey))
            {
                var (project, symbol) = await TryResolveMetadataSymbolAsync(workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
                if (symbol is { Kind: not SymbolKind.Namespace })
                {
                    Contract.ThrowIfNull(project);

                    var navigationService = workspace.Services.GetRequiredService<ISymbolNavigationService>();
                    return await navigationService.GetNavigableLocationAsync(
                        symbol, project, cancellationToken).ConfigureAwait(false);
                }

                return null;
            }

            return await SourceSpans[0].GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
