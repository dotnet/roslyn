// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using TPL = System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.GotoDefinition
{
    internal class RoslynGotoDefinitionService : IGoToDefinitionService
    {
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly RemoteLanguageServiceWorkspace _remoteWorkspace;
        private readonly IThreadingContext _threadingContext;

        public RoslynGotoDefinitionService(
            IStreamingFindUsagesPresenter streamingPresenter,
            AbstractLspClientServiceFactory roslynLspClientServiceFactory,
            RemoteLanguageServiceWorkspace remoteWorkspace,
            IThreadingContext threadingContext)
        {
            _streamingPresenter = streamingPresenter ?? throw new ArgumentNullException(nameof(streamingPresenter));
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _remoteWorkspace = remoteWorkspace ?? throw new ArgumentNullException(nameof(remoteWorkspace));
            _threadingContext = threadingContext ?? throw new ArgumentNullException(nameof(threadingContext));
        }

        public async TPL.Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var definitionItems = await GetDefinitionItemsAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (definitionItems.IsDefaultOrEmpty)
            {
                return ImmutableArray<INavigableItem>.Empty;
            }

            var navigableItems = ImmutableArray.CreateBuilder<INavigableItem>();
            foreach (var documentSpan in definitionItems.SelectMany(di => di.SourceSpans))
            {
                var declaredSymbolInfo = new DeclaredSymbolInfo(Roslyn.Utilities.StringTable.GetInstance(),
                                                string.Empty,
                                                string.Empty,
                                                string.Empty,
                                                string.Empty,
                                                DeclaredSymbolInfoKind.Class,
                                                Accessibility.NotApplicable,
                                                documentSpan.SourceSpan,
                                                ImmutableArray<string>.Empty);

                navigableItems.Add(NavigableItemFactory.GetItemFromDeclaredSymbolInfo(declaredSymbolInfo, documentSpan.Document));
            }

            return navigableItems.ToArray();
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            return _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                var definitionItems = await GetDefinitionItemsAsync(document, position, cancellationToken).ConfigureAwait(true);
                return await _streamingPresenter.TryNavigateToOrPresentItemsAsync(document.Project.Solution.Workspace,
                                                                                      "GoTo Definition",
                                                                                      definitionItems).ConfigureAwait(true);
            });
        }

        private async TPL.Task<ImmutableArray<DefinitionItem>> GetDefinitionItemsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return ImmutableArray<DefinitionItem>.Empty;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textDocumentPositionParams = ProtocolConversions.PositionToTextDocumentPositionParams(position, text, document);

            var response = await lspClient.RequestAsync(LSP.Methods.TextDocumentDefinition.ToLSRequest(), textDocumentPositionParams, cancellationToken).ConfigureAwait(false);
            var locations = ((JToken)response)?.ToObject<LSP.Location[]>();
            if (locations == null)
            {
                return ImmutableArray<DefinitionItem>.Empty;
            }

            var definitionItems = ImmutableArray.CreateBuilder<DefinitionItem>();
            foreach (var location in locations)
            {
                DocumentSpan? documentSpan;
                if (lspClient.ProtocolConverter.IsExternalDocument(location.Uri))
                {
                    var externalDocument = _remoteWorkspace.GetOrAddExternalDocument(location.Uri.LocalPath, document.Project.Language);
                    var externalText = await externalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var textSpan = ProtocolConversions.RangeToTextSpan(location.Range, externalText);
                    documentSpan = new DocumentSpan(externalDocument, textSpan);
                }
                else
                {
                    documentSpan = await _remoteWorkspace.GetDocumentSpanFromLocation(location, cancellationToken).ConfigureAwait(false);
                    if (documentSpan == null)
                    {
                        continue;
                    }
                }

                definitionItems.Add(DefinitionItem.Create(ImmutableArray<string>.Empty, ImmutableArray<TaggedText>.Empty, documentSpan.Value));
            }

            return definitionItems.ToImmutable();
        }
    }
}
