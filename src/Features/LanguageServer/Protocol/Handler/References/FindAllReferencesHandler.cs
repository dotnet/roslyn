// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspMethod(LSP.Methods.TextDocumentReferencesName, mutatesSolutionState: false), Shared]
    internal class FindAllReferencesHandler : IRequestHandler<LSP.ReferenceParams, LSP.VSReferenceItem[]?>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandler(IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(ReferenceParams request) => request.TextDocument;

        public async Task<LSP.VSReferenceItem[]?> HandleRequestAsync(ReferenceParams referenceParams, RequestContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(context.ClientCapabilities.HasVisualStudioLspCapability());

            var document = context.Document;
            if (document == null)
            {
                return Array.Empty<LSP.VSReferenceItem>();
            }

            using var progress = BufferedProgress.Create<VSReferenceItem>(referenceParams.PartialResultToken);

            var findUsagesService = document.GetRequiredLanguageService<IFindUsagesLSPService>();
            var position = await document.GetPositionFromLinePositionAsync(
                ProtocolConversions.PositionToLinePosition(referenceParams.Position), cancellationToken).ConfigureAwait(false);

            var findUsagesContext = new FindUsagesLSPContext(
                progress, document, position, _metadataAsSourceFileService, cancellationToken);

            // Finds the references for the symbol at the specific position in the document, reporting them via streaming to the LSP client.
            await findUsagesService.FindReferencesAsync(document, position, findUsagesContext).ConfigureAwait(false);
            await findUsagesContext.OnCompletedAsync().ConfigureAwait(false);

            return progress.GetValues();
        }
    }
}
