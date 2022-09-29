// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
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
        private sealed class NavigableSymbolSource : INavigableSymbolSource
        {
            private readonly NavigableSymbolService _service;
            private readonly ITextView _textView;

            private bool _disposed;

            public NavigableSymbolSource(
                NavigableSymbolService service,
                ITextView textView)
            {
                _service = service;
                _textView = textView;
            }

            public void Dispose()
                => _disposed = true;

            public async Task<INavigableSymbol?> GetNavigableSymbolAsync(SnapshotSpan triggerSpan, CancellationToken cancellationToken)
            {
                if (_disposed)
                    return null;

                var snapshot = triggerSpan.Snapshot;
                var position = triggerSpan.Start;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return null;

                var service = document.GetLanguageService<IAsyncGoToDefinitionService>();
                if (service == null)
                    return null;

                var (navigableLocation, symbolSpan) = await service.FindDefinitionLocationAsync(
                    document, position, includeType: false, cancellationToken).ConfigureAwait(false);
                if (navigableLocation == null)
                    return null;

                var indicatorFactory = document.Project.Solution.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();

                return new NavigableSymbol(
                    _service,
                    _textView,
                    navigableLocation,
                    snapshot.GetSpan(symbolSpan.ToSpan()),
                    indicatorFactory);
            }
        }
    }
}
