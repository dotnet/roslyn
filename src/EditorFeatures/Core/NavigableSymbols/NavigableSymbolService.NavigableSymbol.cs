// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
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
            private readonly SnapshotSpan _span;
            private readonly Document _document;
            private readonly IStreamingFindUsagesPresenter _presenter;
            private readonly IWaitIndicator _waitIndicator;

            public NavigableSymbol(
                ImmutableArray<DefinitionItem> definitions,
                SnapshotSpan span,
                Document document,
                IStreamingFindUsagesPresenter streamingPresenter,
                IWaitIndicator waitIndicator)
            {
                Contract.ThrowIfFalse(definitions.Length > 0);

                _definitions = definitions;
                _span = span;
                _document = document;
                _presenter = streamingPresenter;
                _waitIndicator = waitIndicator;
            }

            public SnapshotSpan SymbolSpan => _span;

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
                        _document.Project,
                        _definitions[0].NameDisplayParts.GetFullText(),
                        _presenter,
                        context.CancellationToken)
                    );
        }
    }
}
