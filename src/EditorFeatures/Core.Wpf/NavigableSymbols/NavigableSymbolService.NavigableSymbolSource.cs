// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    internal partial class NavigableSymbolService
    {
        private partial class NavigableSymbolSource : INavigableSymbolSource
        {
            private readonly IThreadingContext _threadingContext;
            private readonly IStreamingFindUsagesPresenter _presenter;
            private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
            private readonly IAsynchronousOperationListenerProvider _listenerProvider;

            private bool _disposed;

            public NavigableSymbolSource(
                IThreadingContext threadingContext,
                IStreamingFindUsagesPresenter streamingPresenter,
                IUIThreadOperationExecutor uiThreadOperationExecutor,
                IAsynchronousOperationListenerProvider listenerProvider)
            {
                _threadingContext = threadingContext;
                _presenter = streamingPresenter;
                _uiThreadOperationExecutor = uiThreadOperationExecutor;
                _listenerProvider = listenerProvider;
            }

            public void Dispose()
                => _disposed = true;

            public async Task<INavigableSymbol> GetNavigableSymbolAsync(SnapshotSpan triggerSpan, CancellationToken cancellationToken)
            {
                if (_disposed)
                    return null;

                var snapshot = triggerSpan.Snapshot;
                var position = triggerSpan.Start;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return null;

                var service = document.GetLanguageService<IGoToSymbolService>();
                if (service == null)
                    return null;

                var context = new GoToSymbolContext(document, position, cancellationToken);

                await service.GetSymbolsAsync(context).ConfigureAwait(false);

                if (!context.TryGetItems(WellKnownSymbolTypes.Definition, out var definitions))
                    return null;

                var snapshotSpan = new SnapshotSpan(snapshot, context.Span.ToSpan());
                return new NavigableSymbol(
                    document.Project.Solution.Workspace,
                    definitions.ToImmutableArray(),
                    snapshotSpan,
                    _threadingContext,
                    _presenter,
                    _uiThreadOperationExecutor,
                    _listenerProvider);
            }
        }
    }
}
