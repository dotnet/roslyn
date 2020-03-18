// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.References
{
    internal class RoslynFindUsagesService : IFindUsagesService
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;

        public RoslynFindUsagesService(AbstractLspClientServiceFactory roslynLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _remoteLanguageServiceWorkspace = remoteLanguageServiceWorkspace ?? throw new ArgumentNullException(nameof(remoteLanguageServiceWorkspace));
        }

        public Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            // Find implementations is now handled by the VS LSP client.
            return Task.CompletedTask;
        }

        public async Task FindReferencesAsync(Document document, int position, IFindUsagesContext context)
        {
            var text = await document.GetTextAsync().ConfigureAwait(false);

            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var referenceParams = new LSP.ReferenceParams
            {
                Context = new LSP.ReferenceContext { IncludeDeclaration = false },
                TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document),
                Position = ProtocolConversions.LinePositionToPosition(text.Lines.GetLinePosition(position))
            };

            var locations = await lspClient.RequestAsync(LSP.Methods.TextDocumentReferences.ToLSRequest(), referenceParams, context.CancellationToken).ConfigureAwait(false);
            if (locations == null)
            {
                return;
            }

            // TODO: Need to get real definition data from the server.
            var dummyDef = DefinitionItem.CreateNonNavigableItem(ImmutableArray<string>.Empty, ImmutableArray<TaggedText>.Empty);
            await context.OnDefinitionFoundAsync(dummyDef).ConfigureAwait(false);

            foreach (var location in locations)
            {
                var documentSpan = await _remoteLanguageServiceWorkspace.GetDocumentSpanFromLocationAsync(location, context.CancellationToken).ConfigureAwait(false);
                if (documentSpan == null)
                {
                    continue;
                }

                await context.OnReferenceFoundAsync(new SourceReferenceItem(dummyDef, documentSpan.Value)).ConfigureAwait(false);
            }
        }
    }
}
