// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.References
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

        public async Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            var text = await document.GetTextAsync().ConfigureAwait(false);

            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var documentPositionParams = ProtocolConversions.PositionToTextDocumentPositionParams(position, text, document);

            var response = await lspClient.RequestAsync(LSP.Methods.TextDocumentImplementation.ToLSRequest(), documentPositionParams, context.CancellationToken).ConfigureAwait(false);
            var locations = ((JToken)response)?.ToObject<LSP.Location[]>();
            if (locations == null)
            {
                return;
            }

            foreach (var location in locations)
            {
                var documentSpan = await _remoteLanguageServiceWorkspace.GetDocumentSpanFromLocation(location, context.CancellationToken).ConfigureAwait(false);
                if (documentSpan == null)
                {
                    continue;
                }

                // Get the text for the line containing the definition to show in the UI.
                var docText = await documentSpan.Value.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
                var lineText = docText.GetSubText(docText.Lines[location.Range.Start.Line].Span).ToString();

                await context.OnDefinitionFoundAsync(DefinitionItem.Create(ImmutableArray<string>.Empty,
                    ImmutableArray.Create(new TaggedText(TextTags.Text, lineText)), documentSpan.Value)).ConfigureAwait(false);
            }
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
                var documentSpan = await _remoteLanguageServiceWorkspace.GetDocumentSpanFromLocation(location, context.CancellationToken).ConfigureAwait(false);
                if (documentSpan == null)
                {
                    continue;
                }

#pragma warning disable CS0612 // Type or member is obsolete.  TODO.
                await context.OnReferenceFoundAsync(new SourceReferenceItem(dummyDef, documentSpan.Value, isWrittenTo: false)).ConfigureAwait(false);
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }
    }
}
