// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionItem"/> that sits on top of a 
        /// <see cref="DocumentSpan"/>.
        /// </summary>
        // internal for testing purposes.
        internal sealed class DefaultDefinitionItem : DefinitionItem
        {
            internal sealed override bool IsExternal => false;

            public DefaultDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> nameDisplayParts,
                ImmutableArray<TaggedText> originationParts,
                ImmutableArray<DocumentSpan> sourceSpans,
                ImmutableDictionary<string, string>? properties,
                ImmutableDictionary<string, string>? displayableProperties,
                bool displayIfNoReferences)
                : base(tags, displayParts, nameDisplayParts, originationParts,
                       sourceSpans, properties, displayableProperties, displayIfNoReferences)
            {
            }

            public sealed override async Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                {
                    var (_, symbol) = await TryResolveSymbolInCurrentSolutionAsync(workspace, symbolKey, cancellationToken).ConfigureAwait(false);
                    return symbol is { Kind: not SymbolKind.Namespace };
                }

                return await SourceSpans[0].CanNavigateToAsync(cancellationToken).ConfigureAwait(false);
            }

            public sealed override async Task<bool> TryNavigateToAsync(Workspace workspace, NavigationOptions options, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                {
                    var (project, symbol) = await TryResolveSymbolInCurrentSolutionAsync(workspace, symbolKey, cancellationToken).ConfigureAwait(false);
                    if (symbol is { Kind: not SymbolKind.Namespace })
                    {
                        Contract.ThrowIfNull(project);

                        var navigationService = workspace.Services.GetRequiredService<ISymbolNavigationService>();
                        return await navigationService.TryNavigateToSymbolAsync(
                            symbol, project, options with { PreferProvisionalTab = true }, cancellationToken).ConfigureAwait(false);
                    }

                    return false;
                }

                return await SourceSpans[0].TryNavigateToAsync(options, cancellationToken).ConfigureAwait(false);
            }

            public DetachedDefinitionItem Detach()
                => new(Tags, DisplayParts, NameDisplayParts, OriginationParts, SourceSpans, Properties, DisplayableProperties, DisplayIfNoReferences);

            private async ValueTask<(Project? project, ISymbol? symbol)> TryResolveSymbolInCurrentSolutionAsync(Workspace workspace, string symbolKey, CancellationToken cancellationToken)
            {
                if (!Properties.TryGetValue(MetadataSymbolOriginatingProjectIdGuid, out var projectIdGuid) ||
                    !Properties.TryGetValue(MetadataSymbolOriginatingProjectIdDebugName, out var projectDebugName))
                {
                    return default;
                }

                var project = workspace.CurrentSolution.GetProject(ProjectId.CreateFromSerialized(Guid.Parse(projectIdGuid), projectDebugName));
                if (project == null)
                    return default;

                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbol = SymbolKey.ResolveString(symbolKey, compilation, cancellationToken: cancellationToken).Symbol;
                return (project, symbol);
            }
        }
    }
}
