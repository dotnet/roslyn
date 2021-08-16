// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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
                ImmutableDictionary<string, string> displayableProperties,
                bool displayIfNoReferences)
                : base(tags, displayParts, nameDisplayParts, originationParts,
                       sourceSpans, properties, displayableProperties, displayIfNoReferences)
            {
            }

            private async Task<DocumentSpan?> TryGetDocumentSpanAsync(Workspace workspace, CancellationToken cancellationToken)
            {
                var solution = workspace.CurrentSolution;
                var documentId = SourceSpans[0].DocumentId;
                var document = solution.GetDocument(documentId) ??
                               await solution.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                if (document == null)
                    return null;

                return new DocumentSpan(document, SourceSpans[0].SourceSpan);
            }

            [Obsolete]
            public override bool CanNavigateTo(Workspace workspace, CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable;

            [Obsolete]
            public override bool TryNavigateTo(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable;

            public override async Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return CanNavigateToMetadataSymbol(workspace, symbolKey);

                return await TryGetDocumentSpanAsync(workspace, cancellationToken).ConfigureAwait(false) is DocumentSpan span &&
                       span.CanNavigateTo(cancellationToken);
            }

            public override async Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return TryNavigateToMetadataSymbol(workspace, symbolKey);

                return await TryGetDocumentSpanAsync(workspace, cancellationToken).ConfigureAwait(false) is DocumentSpan span &&
                       span.TryNavigateTo(showInPreviewTab, activateTab, cancellationToken);
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

                var symbol = SymbolKey.ResolveString(symbolKey, compilation).Symbol;
                return (project, symbol);
            }
        }
    }
}
