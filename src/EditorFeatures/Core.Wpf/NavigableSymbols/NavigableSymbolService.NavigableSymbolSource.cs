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

            var definitionLocation = await definitionLocationService.GetDefinitionLocationAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (definitionLocation == null)
                return null;

            var indicatorFactory = document.Project.Solution.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();

            return new NavigableSymbol(
                service,
                textView,
                definitionLocation.Location,
                snapshot.GetSpan(definitionLocation.Span.SourceSpan.ToSpan()),
                indicatorFactory);
        }
    }
}
