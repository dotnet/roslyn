// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.LanguageServiceIndexFormat;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using VS.IntelliNav.Contracts;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    [ExportWorkspaceService(typeof(IFindSymbolMonikerUsagesService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioFindSymbolMonikerUsagesService : AbstractFindSymbolMonikerUsagesService
    {
        private readonly ICodeIndexProvider? _codeIndexProvider;

        [ImportingConstructor]
        public VisualStudioFindSymbolMonikerUsagesService(
            [Import(AllowDefault = true)] ICodeIndexProvider? codeIndexProvider)
        {
            _codeIndexProvider = codeIndexProvider;
        }

        public override async IAsyncEnumerable<ExternalReferenceItem> FindReferencesByMoniker(
            DefinitionItem definition, ImmutableArray<SymbolMoniker> monikers,
            IStreamingProgressTracker progress, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_codeIndexProvider == null)
                yield break;

            var convertedMonikers = ConvertMonikers(monikers);
            var currentPage = 0;
            while (true)
            {
                var referenceItems = await FindReferencesByMonikerAsync(
                    _codeIndexProvider, definition, convertedMonikers, progress, currentPage, cancellationToken).ConfigureAwait(false);

                // If we got no items, we're done.
                if (referenceItems.Length == 0)
                    break;

                foreach (var item in referenceItems)
                    yield return item;

                // Otherwise, we got some items.  Return them to our caller and attempt to retrieve
                // another page.
                currentPage++;
            }
        }

        private async Task<ImmutableArray<ExternalReferenceItem>> FindReferencesByMonikerAsync(
            ICodeIndexProvider codeIndexProvider, DefinitionItem definition, ImmutableArray<ISymbolMoniker> monikers,
            IStreamingProgressTracker progress, int pageIndex, CancellationToken cancellationToken)
        {
            try
            {
                // Let the find-refs window know we have outstanding work
                await progress.AddItemsAsync(1).ConfigureAwait(false);

                var results = await codeIndexProvider.FindReferencesByMonikerAsync(
                    monikers, includeDecleration: false, pageIndex: pageIndex, cancellationToken: cancellationToken).ConfigureAwait(false);

                using var _ = ArrayBuilder<ExternalReferenceItem>.GetInstance(out var referenceItems);

                foreach (var result in results)
                    referenceItems.Add(ConvertResult(definition, result));

                return referenceItems.ToImmutable();
            }
            finally
            {
                // Mark that our async work is done.
                await progress.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        private ExternalReferenceItem ConvertResult(DefinitionItem definition, string result)
        {
            // todo: shape looks like this:

            //{
            //    "uri": "file:///c:/src/test/MyProject/test.cs",
            //    "range": { "start": { "line": 0, "character": 4 }, "end": { "line": 0, "character": 11 } },
            //    "projectName": "MyProject",
            //    "displayPath": "test/MyProject/test.cs",
            //    "text" : "this is a line preview"
            //}

            throw new NotImplementedException();
        }

        private ImmutableArray<ISymbolMoniker> ConvertMonikers(ImmutableArray<SymbolMoniker> monikers)
            => monikers.SelectAsArray(ConvertMoniker);

        private ISymbolMoniker ConvertMoniker(SymbolMoniker moniker)
            => new MonikerWrapper(moniker);

        private class MonikerWrapper : ISymbolMoniker
        {
            private readonly SymbolMoniker _moniker;

            public MonikerWrapper(SymbolMoniker moniker)
                => _moniker = moniker;

            public string Scheme => _moniker.Scheme;

            public string Identifier => _moniker.Identifier;

            public IPackageInformation? PackageInformation => null;
        }
    }
}
