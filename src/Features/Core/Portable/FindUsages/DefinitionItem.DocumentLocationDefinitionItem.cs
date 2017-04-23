// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
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
            private readonly Workspace _workspaceOpt;

            internal override bool IsExternal => false;

            public DefaultDefinitionItem(
                Workspace workspaceOpt,
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
                if (Properties.ContainsKey(MetadataSymbolKey))
                {
                    Contract.ThrowIfFalse(Properties.ContainsKey(MetadataAssemblyIdentityDisplayName));
                    Contract.ThrowIfNull(workspaceOpt);
                }

                _workspaceOpt = workspaceOpt;
            }

            public override bool CanNavigateTo()
            {
                if (this.Properties.ContainsKey(NonNavigable))
                {
                    return false;
                }

                if (this.Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                {
                    return CanNavigateToMetadataSymbol(symbolKey);
                }

                return SourceSpans[0].CanNavigateTo();
            }

            public override bool TryNavigateTo()
            {
                if (this.Properties.ContainsKey(NonNavigable))
                {
                    return false;
                }

                if (this.Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                {
                    return TryNavigateToMetadataSymbol(symbolKey);
                }

                return SourceSpans[0].TryNavigateTo();
            }

            private bool CanNavigateToMetadataSymbol(string symbolKey)
                => TryNavigateToMetadataSymbol(symbolKey, action: (symbol, project, service) => true);

            private bool TryNavigateToMetadataSymbol(string symbolKey)
            {
                return TryNavigateToMetadataSymbol(symbolKey,
                    action: (symbol, project, service) =>
                    {
                        return service.TryNavigateToSymbol(
                            symbol, project, project.Solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
                    });
            }

            private bool TryNavigateToMetadataSymbol(string symbolKey, Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var projectAndSymbol = TryResolveSymbolInCurrentSolution(symbolKey);

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

                // For metadata-definitions, it's a requirement that we always have a workspace.
                var navigationService = _workspaceOpt.Services.GetService<ISymbolNavigationService>();
                return action(symbol, project, navigationService);
            }

            private (Project project, ISymbol symbol) TryResolveSymbolInCurrentSolution(string symbolKey)
            {
                if (!this.Properties.TryGetValue(MetadataAssemblyIdentityDisplayName, out var identityDisplayName) ||
                    !AssemblyIdentity.TryParseDisplayName(identityDisplayName, out var identity))
                {
                    return (null, null);
                }

                // For metadata-definitions, it's a requirement that we always have a workspace.
                var project = _workspaceOpt.CurrentSolution
                    .ProjectsWithReferenceToAssembly(identity)
                    .FirstOrDefault();

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