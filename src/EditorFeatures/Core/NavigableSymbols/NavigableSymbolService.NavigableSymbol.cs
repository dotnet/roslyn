// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    internal partial class NavigableSymbolService
    {
        private class NavigableSymbol : INavigableSymbol
        {
            private readonly ImmutableArray<DefinitionItem> _definitions;
            private readonly Document _document;
            private readonly IThreadingContext _threadingContext;
            private readonly IStreamingFindUsagesPresenter _presenter;
            private readonly IWaitIndicator _waitIndicator;

            public NavigableSymbol(
                ImmutableArray<DefinitionItem> definitions,
                SnapshotSpan symbolSpan,
                Document document,
                IThreadingContext threadingContext,
                IStreamingFindUsagesPresenter streamingPresenter,
                IWaitIndicator waitIndicator)
            {
                Contract.ThrowIfFalse(definitions.Length > 0);

                _definitions = definitions;
                _document = document;
                SymbolSpan = symbolSpan;
                _threadingContext = threadingContext;
                _presenter = streamingPresenter;
                _waitIndicator = waitIndicator;
            }

            public SnapshotSpan SymbolSpan { get; }

            public IEnumerable<INavigableRelationship> Relationships =>
                SpecializedCollections.SingletonEnumerable(PredefinedNavigableRelationships.Definition);

            public void Navigate(INavigableRelationship relationship) =>
                _waitIndicator.Wait(
                    title: EditorFeaturesResources.Go_to_Definition,
                    message: EditorFeaturesResources.Navigating_to_definition,
                    allowCancel: true,
                    showProgress: false,
                    action: context => GoToDefinitionHelpers.TryGoToDefinition(
                        _definitions,
                        _document.Project.Solution,
                        _definitions[0].NameDisplayParts.GetFullText(),
                        _threadingContext,
                        _presenter));
        }
    }
}
