// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    internal partial class NavigableSymbolService
    {
        private class NavigableSymbol : INavigableSymbol
        {
            private readonly NavigableSymbolService _service;
            private readonly INavigableLocation _location;

            public NavigableSymbol(
                NavigableSymbolService service,
                INavigableLocation location,
                SnapshotSpan symbolSpan)
            {
                Contract.ThrowIfNull(location);

                _service = service;
                _location = location;
                SymbolSpan = symbolSpan;
            }

            public SnapshotSpan SymbolSpan { get; }

            public IEnumerable<INavigableRelationship> Relationships =>
                SpecializedCollections.SingletonEnumerable(PredefinedNavigableRelationships.Definition);

            public void Navigate(INavigableRelationship relationship)
            {
                // Fire and forget.
                var token = _service._listener.BeginAsyncOperation(nameof(NavigateAsync));
                _ = NavigateAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
            }

            private async Task NavigateAsync()
            {
                using var context = _service._uiThreadOperationExecutor.BeginExecute(
                    title: EditorFeaturesResources.Go_to_Definition,
                    defaultDescription: EditorFeaturesResources.Navigating_to_definition,
                    allowCancellation: true,
                    showProgress: false);

                await _location.NavigateToAsync(
                    new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true),
                    context.UserCancellationToken).ConfigureAwait(false);
            }
        }
    }
}
