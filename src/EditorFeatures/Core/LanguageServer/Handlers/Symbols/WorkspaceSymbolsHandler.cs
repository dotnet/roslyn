// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once
    /// we no longer reference VS icon types.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(WorkspaceSymbolsHandler)), Shared]
    [Method(Methods.WorkspaceSymbolName)]
    internal class WorkspaceSymbolsHandler : AbstractStatelessRequestHandler<WorkspaceSymbolParams, SymbolInformation[]?>
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
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceSymbolsHandler(
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
        {
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
            _threadingContext = threadingContext;
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceSymbolParams request) => null;

        public override async Task<SymbolInformation[]?> HandleRequestAsync(WorkspaceSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            var solution = context.Solution;

            using var progress = BufferedProgress.Create(request.PartialResultToken);
            var searcher = NavigateToSearcher.Create(
                solution,
                _asyncListener,
                new LSPNavigateToCallback(context, progress),
                request.Query,
                s_supportedKinds,
                _threadingContext.DisposalToken);

            await searcher.SearchAsync(searchCurrentDocument: false, cancellationToken).ConfigureAwait(false);
            return progress.GetValues();
        }

        private class LSPNavigateToCallback : INavigateToSearchCallback
        {
            private readonly RequestContext _context;
            private readonly BufferedProgress<SymbolInformation> _progress;

            public LSPNavigateToCallback(
                RequestContext context,
                BufferedProgress<SymbolInformation> progress)
            {
                _context = context;
                _progress = progress;
            }

            public async Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                var location = await ProtocolConversions.TextSpanToLocationAsync(
                    result.NavigableItem.Document, result.NavigableItem.SourceSpan, result.NavigableItem.IsStale, _context, cancellationToken).ConfigureAwait(false);
                if (location == null)
                    return;

                _progress.Report(new VSSymbolInformation
                {
                    Name = result.Name,
                    ContainerName = result.AdditionalInformation,
                    Kind = ProtocolConversions.NavigateToKindToSymbolKind(result.Kind),
                    Location = location,
                    Icon = VSLspExtensionConversions.GetImageIdFromGlyph(result.NavigableItem.Glyph)
                });
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
        }
    }
}
