// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.WorkspaceSymbolName, mutatesSolutionState: false)]
    internal class WorkspaceSymbolsHandler : IRequestHandler<WorkspaceSymbolParams, SymbolInformation[]?>
    {
        private static readonly IImmutableSet<string> s_supportedKinds =
            ImmutableHashSet.Create(
                NavigateToItemKind.Class,
                NavigateToItemKind.Constant,
                NavigateToItemKind.Delegate,
                NavigateToItemKind.Enum,
                NavigateToItemKind.EnumItem,
                NavigateToItemKind.Event,
                NavigateToItemKind.Field,
                NavigateToItemKind.Interface,
                NavigateToItemKind.Method,
                NavigateToItemKind.Module,
                NavigateToItemKind.Property,
                NavigateToItemKind.Structure);

        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceSymbolsHandler(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceSymbolParams request) => null;

        public async Task<SymbolInformation[]?> HandleRequestAsync(WorkspaceSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;

            using var progress = BufferedProgress.Create(request.PartialResultToken);
            var searcher = new NavigateToSearcher(
                solution,
                _asyncListener,
                new LSPNavigateToCallback(progress),
                request.Query,
                searchCurrentDocument: false,
                s_supportedKinds,
                cancellationToken);

            await searcher.SearchAsync().ConfigureAwait(false);

            return progress.GetValues();
        }

        private class LSPNavigateToCallback : INavigateToSearchCallback
        {
            private readonly BufferedProgress<SymbolInformation> _progress;

            public LSPNavigateToCallback(BufferedProgress<SymbolInformation> progress)
            {
                _progress = progress;
            }

            public Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                return ReportSymbolInformationAsync(result, cancellationToken);
            }

            public void Done(bool isFullyLoaded)
            {
                // do nothing, we already await the SearchAsync method which calls this in a finally right before returning.
                // used by non-LSP editor API.
            }

            public void ReportProgress(int current, int maximum)
            {
                // do nothing, LSP doesn't support reporting progress towards completion.
                // used by non-LSP editor API.
            }

            private async Task ReportSymbolInformationAsync(INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                var location = await ProtocolConversions.TextSpanToLocationAsync(result.NavigableItem.Document, result.NavigableItem.SourceSpan, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(location);
                _progress.Report(new VSSymbolInformation
                {
                    Name = result.Name,
                    ContainerName = result.AdditionalInformation,
                    Kind = ProtocolConversions.NavigateToKindToSymbolKind(result.Kind),
                    Location = location,
                    Icon = new ImageElement(result.NavigableItem.Glyph.GetImageId())
                });
            }
        }
    }
}
