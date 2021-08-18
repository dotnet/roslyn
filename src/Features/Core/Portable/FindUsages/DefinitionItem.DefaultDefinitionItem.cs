// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
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
            public sealed override bool CanNavigateTo(Workspace workspace, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return CanNavigateToMetadataSymbol(workspace, symbolKey, Properties);

                return SourceSpans[0].CanNavigateTo(cancellationToken);
            }

            public sealed override bool TryNavigateTo(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return TryNavigateToMetadataSymbol(workspace, symbolKey, Properties);

                return SourceSpans[0].TryNavigateTo(showInPreviewTab, activateTab, cancellationToken);
            }

            public DetachedDefinitionItem Detach()
                => new(Tags, DisplayParts, NameDisplayParts, OriginationParts, SourceSpans, Properties, DisplayableProperties, DisplayIfNoReferences);

            private static bool CanNavigateToMetadataSymbol(Workspace workspace, string symbolKey, ImmutableDictionary<string, string> properties)
                => TryNavigateToMetadataSymbol(workspace, symbolKey, properties, action: (symbol, project, service) => true);

            private static bool TryNavigateToMetadataSymbol(Workspace workspace, string symbolKey, ImmutableDictionary<string, string> properties)
            {
                return TryNavigateToMetadataSymbol(
                    workspace, symbolKey, properties,
                    action: (symbol, project, service) =>
                    {
                        return service.TryNavigateToSymbol(
                            symbol, project, project.Solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
                    });
            }

            private static bool TryNavigateToMetadataSymbol(
                Workspace workspace, string symbolKey, ImmutableDictionary<string, string> properties, Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var projectAndSymbol = TryResolveSymbolInCurrentSolution(workspace, symbolKey, properties);

                var project = projectAndSymbol.project;
                var symbol = projectAndSymbol.symbol;
                if (symbol == null || project == null)
                {
                    return false;
                }

                if (symbol.Kind == SymbolKind.Namespace)
                {
                    return false;
                }

                var navigationService = workspace.Services.GetService<ISymbolNavigationService>();
                return action(symbol, project, navigationService);
            }

            private static (Project project, ISymbol symbol) TryResolveSymbolInCurrentSolution(
                Workspace workspace, string symbolKey, ImmutableDictionary<string, string> properties)
            {
                if (!properties.TryGetValue(MetadataSymbolOriginatingProjectIdGuid, out var projectIdGuid) ||
                    !properties.TryGetValue(MetadataSymbolOriginatingProjectIdDebugName, out var projectDebugName))
                {
                    return default;
                }

                var project = workspace.CurrentSolution.GetProject(ProjectId.CreateFromSerialized(Guid.Parse(projectIdGuid), projectDebugName));
                if (project == null)
                    return default;

                var compilation = project.GetCompilationAsync(CancellationToken.None)
                                         .WaitAndGetResult(CancellationToken.None);

                var symbol = SymbolKey.ResolveString(symbolKey, compilation).Symbol;
                return (project, symbol);
            }
        }
    }
}
