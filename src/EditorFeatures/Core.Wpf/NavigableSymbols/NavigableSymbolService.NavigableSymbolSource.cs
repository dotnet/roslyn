// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols;

internal partial class NavigableSymbolService
{
    private sealed class NavigableSymbolSource(
        NavigableSymbolService service,
        ITextView textView) : INavigableSymbolSource
    {
        private bool _disposed;

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

            var definitionLocationService = document.GetLanguageService<IDefinitionLocationService>();
            if (definitionLocationService == null)
                return null;

            var indicatorFactory = document.Project.Solution.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();

            // We want to compute this as quickly as possible so that the symbol be squiggled and navigated to.  We
            // don't want to wait on expensive operations like computing source-generators or skeletons if we can avoid
            // it.  So first try with a frozen document, then fallback to a normal document.  This mirrors how go-to-def
            // works as well.
            return
                await GetNavigableSymbolWorkerAsync(document.WithFrozenPartialSemantics(cancellationToken)).ConfigureAwait(false) ??
                await GetNavigableSymbolWorkerAsync(document).ConfigureAwait(false);

            async Task<INavigableSymbol?> GetNavigableSymbolWorkerAsync(Document document)
            {
                var definitionLocation = await definitionLocationService.GetDefinitionLocationAsync(
                    document, position, cancellationToken).ConfigureAwait(false);
                if (definitionLocation == null)
                    return null;

                return new NavigableSymbol(
                    service,
                    textView,
                    definitionLocation.Location,
                    snapshot.GetSpan(definitionLocation.Span.SourceSpan.ToSpan()),
                    indicatorFactory);
            }
        }
    }
}
