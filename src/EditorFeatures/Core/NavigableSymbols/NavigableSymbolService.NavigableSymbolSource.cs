﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
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
                return new NavigableSymbol(definitions.ToImmutableArray(), snapshotSpan, document, _presenters, _waitIndicator);
            }
        }
    }
}
