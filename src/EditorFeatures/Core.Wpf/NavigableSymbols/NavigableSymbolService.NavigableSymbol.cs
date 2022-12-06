// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    internal partial class NavigableSymbolService
    {
        private class NavigableSymbol : INavigableSymbol
        {
            private readonly NavigableSymbolService _service;
            private readonly ITextView _textView;
            private readonly INavigableLocation _location;

            public NavigableSymbol(
                NavigableSymbolService service,
                ITextView textView,
                INavigableLocation location,
                SnapshotSpan symbolSpan)
            {
                Contract.ThrowIfNull(location);

                _service = service;
                _textView = textView;
                _location = location;
                SymbolSpan = symbolSpan;
            }

            public SnapshotSpan SymbolSpan { get; }

            public IEnumerable<INavigableRelationship> Relationships
                => SpecializedCollections.SingletonEnumerable(PredefinedNavigableRelationships.Definition);

            public void Navigate(INavigableRelationship relationship)
            {
                // Fire and forget.
                var token = _service._listener.BeginAsyncOperation(nameof(NavigateAsync));
                _ = NavigateAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
            }

            private async Task NavigateAsync()
            {
                // we're about to navigate.  so disable cancellation on focus-lost in our indicator so we don't end up
                // causing ourselves to self-cancel.
                using var backgroundIndicator = _service._backgroundWorkIndicatorService.Create(
                    _textView, SymbolSpan,
                    EditorFeaturesResources.Navigating_to_definition,
                    new BackgroundWorkIndicatorOptions()
                    {
                        CancelOnFocusLost = false,
                        CancelOnEdit = true
                    });

                await _location.TryNavigateToAsync(
                    _service._threadingContext, new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true), backgroundIndicator.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
