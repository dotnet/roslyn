// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    internal partial class NavigableSymbolService
    {
        private partial class NavigableSymbolSource : INavigableSymbolSource
        {
            private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _presenters;
            private readonly IWaitIndicator _waitIndicator;

            private bool _disposed;

            public NavigableSymbolSource(
                IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
                IWaitIndicator waitIndicator)
            {
                _presenters = streamingPresenters;
                _waitIndicator = waitIndicator;
            }

            public void Dispose()
            {
                _disposed = true;
            }

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

                var context = new GoToDefinitionContext(
                    document, 
                    position, 
                    cancellationToken: cancellationToken);

                await service.GetDefinitionsAsync(context).ConfigureAwait(false);

                if (!context.TryGetItems(WellKnownDefinitionTypes.Definition, out var definitions))
                {
                    return null;
                }

                var span = context.Span;
                var snapshotSpan = new SnapshotSpan(snapshot, span.Start, span.Length);
                return new NavigableSymbol(definitions.ToImmutableArray(), snapshotSpan, document, _presenters, _waitIndicator);
            }
        }
    }
}
