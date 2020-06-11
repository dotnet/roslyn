// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    internal partial class NavigableSymbolService
    {
        private partial class NavigableSymbolSource : INavigableSymbolSource
        {
            private readonly IThreadingContext _threadingContext;
            private readonly IStreamingFindUsagesPresenter _presenter;
            private readonly IWaitIndicator _waitIndicator;

            private bool _disposed;

            public NavigableSymbolSource(
                IThreadingContext threadingContext,
                IStreamingFindUsagesPresenter streamingPresenter,
                IWaitIndicator waitIndicator)
            {
                _threadingContext = threadingContext;
                _presenter = streamingPresenter;
                _waitIndicator = waitIndicator;
            }

            public void Dispose()
                => _disposed = true;

            public async Task<INavigableSymbol> GetNavigableSymbolAsync(SnapshotSpan triggerSpan, CancellationToken cancellationToken)
            {
                if (_disposed)
                {
                    return null;
                }

                var snapshot = triggerSpan.Snapshot;
                var position = triggerSpan.Start;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return null;
                }

                var service = document.GetLanguageService<IGoToSymbolService>();
                if (service == null)
                {
                    return null;
                }

                var context = new GoToSymbolContext(document, position, cancellationToken);

                await service.GetSymbolsAsync(context).ConfigureAwait(false);

                if (!context.TryGetItems(WellKnownSymbolTypes.Definition, out var definitions))
                {
                    return null;
                }

                var snapshotSpan = new SnapshotSpan(snapshot, context.Span.ToSpan());
                return new NavigableSymbol(definitions.ToImmutableArray(), snapshotSpan, document, _threadingContext, _presenter, _waitIndicator);
            }
        }
    }
}
