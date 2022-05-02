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
                ImmutableDictionary<string, string> properties,
                ImmutableDictionary<string, string> displayableProperties,
                bool displayIfNoReferences)
                : base(tags, displayParts, nameDisplayParts, originationParts,
                       sourceSpans, properties, displayableProperties, displayIfNoReferences)
            {
            }

            [Obsolete("Override CanNavigateToAsync instead", error: false)]
            public override bool CanNavigateTo(Workspace workspace, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            [Obsolete("Override TryNavigateToAsync instead", error: false)]
            public override bool TryNavigateTo(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public sealed override async Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return await CanNavigateToMetadataSymbolAsync(workspace, symbolKey).ConfigureAwait(false);

                return await this.SourceSpans[0].CanNavigateToAsync(cancellationToken).ConfigureAwait(false);
            }

            public sealed override async Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return await TryNavigateToMetadataSymbolAsync(workspace, symbolKey).ConfigureAwait(false);

                return await this.SourceSpans[0].TryNavigateToAsync(showInPreviewTab, activateTab, cancellationToken).ConfigureAwait(false);
            }

            public DetachedDefinitionItem Detach()
                => new(Tags, DisplayParts, NameDisplayParts, OriginationParts, SourceSpans, Properties, DisplayableProperties, DisplayIfNoReferences);

            private Task<bool> CanNavigateToMetadataSymbolAsync(Workspace workspace, string symbolKey)
                => TryNavigateToMetadataSymbolAsync(workspace, symbolKey, action: (symbol, project, service) => true);

            private Task<bool> TryNavigateToMetadataSymbolAsync(Workspace workspace, string symbolKey)
            {
                return TryNavigateToMetadataSymbolAsync(
                    workspace, symbolKey,
                    action: (symbol, project, service) =>
                    {
                        return service.TryNavigateToSymbol(
                            symbol, project, project.Solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
                    });
            }

            private async Task<bool> TryNavigateToMetadataSymbolAsync(
                Workspace workspace, string symbolKey, Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var projectAndSymbol = await TryResolveSymbolInCurrentSolutionAsync(workspace, symbolKey).ConfigureAwait(false);
                if (projectAndSymbol is not (var project, { Kind: not SymbolKind.Namespace } symbol))
                {
                    return false;
                }

                var navigationService = workspace.Services.GetRequiredService<ISymbolNavigationService>();
                return action(symbol, project, navigationService);
            }

            private async ValueTask<(Project project, ISymbol? symbol)?> TryResolveSymbolInCurrentSolutionAsync(Workspace workspace, string symbolKey)
            {
                if (!Properties.TryGetValue(MetadataSymbolOriginatingProjectIdGuid, out var projectIdGuid) ||
                    !Properties.TryGetValue(MetadataSymbolOriginatingProjectIdDebugName, out var projectDebugName))
                {
                    return null;
                }

                var project = workspace.CurrentSolution.GetProject(ProjectId.CreateFromSerialized(Guid.Parse(projectIdGuid), projectDebugName));
                if (project == null)
                    return null;

                var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None).ConfigureAwait(false);
                var symbol = SymbolKey.ResolveString(symbolKey, compilation).Symbol;
                return (project, symbol);
            }
        }
    }
}
