// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            internal override bool IsExternal => false;

            public DefaultDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> nameDisplayParts,
                ImmutableArray<TaggedText> originationParts,
                ImmutableArray<DocumentSpan> sourceSpans,
                ImmutableDictionary<string, string> properties,
                bool displayIfNoReferences)
                : base(tags, displayParts, nameDisplayParts, originationParts,
                       sourceSpans, properties, displayIfNoReferences)
            {
            }

            public override bool CanNavigateTo(Workspace workspace)
            {
                if (Properties.ContainsKey(NonNavigable))
                {
                    return false;
                }

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                {
                    return CanNavigateToMetadataSymbol(workspace, symbolKey);
                }

                return SourceSpans[0].CanNavigateTo();
            }

            public override bool TryNavigateTo(Workspace workspace, bool isPreview)
            {
                if (Properties.ContainsKey(NonNavigable))
                {
                    return false;
                }

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                {
                    return TryNavigateToMetadataSymbol(workspace, symbolKey);
                }

                return SourceSpans[0].TryNavigateTo(isPreview);
            }

            private bool CanNavigateToMetadataSymbol(Workspace workspace, string symbolKey)
                => TryNavigateToMetadataSymbol(workspace, symbolKey, action: (symbol, project, service) => true);

            private bool TryNavigateToMetadataSymbol(Workspace workspace, string symbolKey)
            {
                return TryNavigateToMetadataSymbol(workspace, symbolKey,
                    action: (symbol, project, service) =>
                    {
                        return service.TryNavigateToSymbol(
                            symbol, project, project.Solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
                    });
            }

            private bool TryNavigateToMetadataSymbol(
                Workspace workspace, string symbolKey, Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var projectAndSymbol = TryResolveSymbolInCurrentSolution(workspace, symbolKey);

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

            private (Project project, ISymbol symbol) TryResolveSymbolInCurrentSolution(
                Workspace workspace, string symbolKey)
            {
                if (!Properties.TryGetValue(MetadataSymbolOriginatingProjectIdGuid, out var projectIdGuid) ||
                    !Properties.TryGetValue(MetadataSymbolOriginatingProjectIdDebugName, out var projectDebugName))
                {
                    return (null, null);
                }

                var project = workspace.CurrentSolution.GetProject(ProjectId.CreateFromSerialized(Guid.Parse(projectIdGuid), projectDebugName));

                if (project == null)
                {
                    return (null, null);
                }

                var compilation = project.GetCompilationAsync(CancellationToken.None)
                                         .WaitAndGetResult(CancellationToken.None);

                var symbol = SymbolKey.Resolve(symbolKey, compilation).Symbol;
                return (project, symbol);
            }
        }
    }
}
