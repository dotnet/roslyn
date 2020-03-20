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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using VS.IntelliNav.Contracts;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    [ExportWorkspaceService(typeof(IFindSymbolMonikerUsagesService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioFindSymbolMonikerUsagesService : AbstractFindSymbolMonikerUsagesService
    {
        private readonly ICodeIndexProvider? _codeIndexProvider;

        private readonly object _gate = new object();
        private CancellationTokenSource? _lastNavigationCancellationSource;

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
            var parsed = JObject.Parse(result);
            var uri = new Uri(parsed.Value<string>("uri"));
            var projectName = parsed.Value<string>("projectName");
            var displayPath = parsed.Value<string>("displayPath");
            var span = ConvertLinePositionSpan(parsed.Value<JObject>("range"));
            var text = parsed.Value<string>("text");

            return new CodeIndexExternalReferenceItem(
                this, definition, uri, projectName, displayPath, span, text);

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

        private class MonikerWrapper : ISymbolMoniker
        {
            private readonly SymbolMoniker _moniker;

            public MonikerWrapper(SymbolMoniker moniker)
                => _moniker = moniker;

            public string Scheme => _moniker.Scheme;

            public string Identifier => _moniker.Identifier;

            public IPackageInformation? PackageInformation => null;
        }

        private class CodeIndexExternalReferenceItem : ExternalReferenceItem
        {
            private readonly VisualStudioFindSymbolMonikerUsagesService _service;
            private readonly Uri _documentUri;

            public CodeIndexExternalReferenceItem(
                VisualStudioFindSymbolMonikerUsagesService service,
                DefinitionItem definition,
                Uri documentUri,
                string projectName,
                string displayPath,
                LinePositionSpan span,
                string text) : base(definition, projectName, displayPath, span, text)
            {
                _service = service;
                _documentUri = documentUri;
            }

            public override bool TryNavigateTo(bool isPreview)
            {
                // Cancel the navigation to any previous item the user was trying to navigate to.
                // Then try to navigate to this. Because it's async, and we're not, just assume it
                // will succeed.
                var cancellationToken = _service.CancelLastNavigationAndGetNavigationToken();
                _ = NavigateToAsync(isPreview: false, cancellationToken);
                return true;
            }

            private async Task NavigateToAsync(bool isPreview, CancellationToken cancellationToken)
            {
                // No way to report any errors thrown by OpenNavigationResultInEditorAsync.
                // So just catch and report through our watson system.
                try
                {
                    await _service._codeIndexProvider!.OpenNavigationResultInEditorAsync(
                        _documentUri, this.Span.Start.Line, this.Span.Start.Character, cancellationToken).ConfigureAwait(false);
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
}
