﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using VS.IntelliNav.Contracts;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    [ExportWorkspaceService(typeof(IFindSymbolMonikerUsagesService), layer: ServiceLayer.Host), Shared]
    internal partial class VisualStudioFindSymbolMonikerUsagesService : AbstractFindSymbolMonikerUsagesService
    {
        private readonly ICodeIndexProvider? _codeIndexProvider;

        private readonly object _gate = new object();
        private CancellationTokenSource? _lastNavigationCancellationSource;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
            // Let the find-refs window know we have outstanding work
            await using var _1 = await progress.AddSingleItemAsync().ConfigureAwait(false);

            var results = await FindReferencesByMonikerAsync(
                codeIndexProvider, monikers, pageIndex, cancellationToken).ConfigureAwait(false);

            using var _2 = ArrayBuilder<ExternalReferenceItem>.GetInstance(out var referenceItems);

            foreach (var result in results)
                referenceItems.Add(ConvertResult(definition, result));

            return referenceItems.ToImmutable();
        }

        private static async Task<ICollection<JObject>> FindReferencesByMonikerAsync(ICodeIndexProvider codeIndexProvider, ImmutableArray<ISymbolMoniker> monikers, int pageIndex, CancellationToken cancellationToken)
        {
            try
            {
                return await codeIndexProvider.FindReferencesByMonikerAsync(
                    monikers, includeDeclaration: false, pageIndex: pageIndex, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return SpecializedCollections.EmptyCollection<JObject>();
            }
        }

        private ExternalReferenceItem ConvertResult(DefinitionItem definition, JObject obj)
        {
            var projectName = obj.Value<string>("projectName");
            var displayPath = obj.Value<string>("displayPath");
            var span = ConvertLinePositionSpan(obj.Value<JObject>("range"));
            var text = obj.Value<string>("text");

            return new CodeIndexExternalReferenceItem(
                this, definition, obj, projectName, displayPath, span, text);

            static LinePositionSpan ConvertLinePositionSpan(JObject obj)
                => new LinePositionSpan(
                    ConvertLinePosition(obj.Value<JObject>("start")),
                    ConvertLinePosition(obj.Value<JObject>("end")));

            static LinePosition ConvertLinePosition(JObject obj)
                => new LinePosition(obj.Value<int>("line"), obj.Value<int>("character"));
        }

        private ImmutableArray<ISymbolMoniker> ConvertMonikers(ImmutableArray<SymbolMoniker> monikers)
            => monikers.SelectAsArray(ConvertMoniker);

        private ISymbolMoniker ConvertMoniker(SymbolMoniker moniker)
            => new MonikerWrapper(moniker);

        private CancellationToken CancelLastNavigationAndGetNavigationToken()
        {
            lock (_gate)
            {
                _lastNavigationCancellationSource?.Cancel();
                _lastNavigationCancellationSource = new CancellationTokenSource();
                return _lastNavigationCancellationSource.Token;
            }
        }

        private bool TryNavigateTo(CodeIndexExternalReferenceItem item, bool isPreview)
        {
            // Cancel the navigation to any previous item the user was trying to navigate to.
            // Then try to navigate to this. Because it's async, and we're not, just assume it
            // will succeed.
            var cancellationToken = this.CancelLastNavigationAndGetNavigationToken();
            _ = NavigateToAsync(item, isPreview, cancellationToken);
            return true;
        }

        private async Task NavigateToAsync(CodeIndexExternalReferenceItem item, bool isPreview, CancellationToken cancellationToken)
        {
            // No way to report any errors thrown by OpenNavigationResultInEditorAsync.
            // So just catch and report through our watson system.
            try
            {
                await _codeIndexProvider!.OpenNavigationResultInEditorAsync(
                    item.ResultObject, isPreview, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }
        }
    }
}
