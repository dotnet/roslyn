// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once
    /// we no longer reference VS icon types.
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(WorkspaceSymbolsHandler)), Shared]
    [Method(Methods.WorkspaceSymbolName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class WorkspaceSymbolsHandler(IAsynchronousOperationListenerProvider listenerProvider)
        : ILspServiceRequestHandler<WorkspaceSymbolParams, SymbolInformation[]?>
    {
        private static readonly IImmutableSet<string> s_supportedKinds = [
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
            NavigateToItemKind.Structure
        ];

        private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public async Task<SymbolInformation[]?> HandleRequestAsync(WorkspaceSymbolParams request, RequestContext context, CancellationToken cancellationToken)
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
                cancellationToken);

            await searcher.SearchAsync(NavigateToSearchScope.Solution, cancellationToken).ConfigureAwait(false);
            return progress.GetFlattenedValues();
        }

        private sealed class LSPNavigateToCallback(
            RequestContext context,
            BufferedProgress<SymbolInformation[]> progress)
            : INavigateToSearchCallback
        {
            public async Task AddResultsAsync(ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(context.Solution);
                var solution = context.Solution;

                foreach (var result in results)
                {
                    var document = await result.NavigableItem.Document.GetRequiredDocumentAsync(solution, cancellationToken).ConfigureAwait(false);

                    var location = await ProtocolConversions.TextSpanToLocationAsync(
                        document, result.NavigableItem.SourceSpan, result.NavigableItem.IsStale, context, cancellationToken).ConfigureAwait(false);
                    if (location == null)
                        return;

                    var service = solution.Services.GetRequiredService<ILspSymbolInformationCreationService>();
                    var symbolInfo = service.Create(
                        result.Name, result.AdditionalInformation, ProtocolConversions.NavigateToKindToSymbolKind(result.Kind), location, result.NavigableItem.Glyph);

                    progress.Report(symbolInfo);
                }
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

            public void ReportIncomplete()
            {
            }
        }
    }
}
