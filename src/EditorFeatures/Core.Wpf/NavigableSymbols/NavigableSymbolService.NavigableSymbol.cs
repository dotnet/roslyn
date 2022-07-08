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
            private readonly Workspace _workspace;
            private readonly ImmutableArray<DefinitionItem> _definitions;
            private readonly IThreadingContext _threadingContext;
            private readonly IStreamingFindUsagesPresenter _presenter;
            private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
            private readonly IAsynchronousOperationListener _listener;

            public NavigableSymbol(
                Workspace workspace,
                ImmutableArray<DefinitionItem> definitions,
                SnapshotSpan symbolSpan,
                IThreadingContext threadingContext,
                IStreamingFindUsagesPresenter streamingPresenter,
                IUIThreadOperationExecutor uiThreadOperationExecutor,
                IAsynchronousOperationListenerProvider listenerProvider)
            {
                Contract.ThrowIfFalse(definitions.Length > 0);

                _workspace = workspace;
                _definitions = definitions;
                SymbolSpan = symbolSpan;
                _threadingContext = threadingContext;
                _presenter = streamingPresenter;
                _uiThreadOperationExecutor = uiThreadOperationExecutor;
                _listener = listenerProvider.GetListener(FeatureAttribute.NavigableSymbols);
            }

            public SnapshotSpan SymbolSpan { get; }

            public IEnumerable<INavigableRelationship> Relationships =>
                SpecializedCollections.SingletonEnumerable(PredefinedNavigableRelationships.Definition);

            public void Navigate(INavigableRelationship relationship)
            {
                // Fire and forget.
                var token = _listener.BeginAsyncOperation(nameof(NavigateAsync));
                _ = NavigateAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
            }

            private async Task NavigateAsync()
            {
                using var context = _uiThreadOperationExecutor.BeginExecute(
                    title: EditorFeaturesResources.Go_to_Definition,
                    defaultDescription: EditorFeaturesResources.Navigating_to_definition,
                    allowCancellation: true,
                    showProgress: false);

                var cancellationToken = context.UserCancellationToken;
                await _presenter.TryPresentLocationOrNavigateIfOneAsync(
                    _threadingContext,
                    _workspace,
                    _definitions[0].NameDisplayParts.GetFullText(),
                    _definitions,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
