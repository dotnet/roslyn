// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once
    /// the <see cref="IFindUsagesService"/> is moved down to the features layer and
    /// we no longer reference VS classified text runs.
    /// See https://github.com/dotnet/roslyn/issues/55142
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(FindAllReferencesHandler)), Shared]
    [Method(LSP.Methods.TextDocumentReferencesName)]
    internal class FindAllReferencesHandler : AbstractStatelessRequestHandler<LSP.ReferenceParams, LSP.VSInternalReferenceItem[]?>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandler(
            IMetadataAsSourceFileService metadataAsSourceFileService,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            IGlobalOptionService globalOptions)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
            _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.LanguageServer);
            _globalOptions = globalOptions;
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(ReferenceParams request) => request.TextDocument;

        public override async Task<LSP.VSInternalReferenceItem[]?> HandleRequestAsync(ReferenceParams referenceParams, RequestContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(context.ClientCapabilities.HasVisualStudioLspCapability());

            var document = context.Document;
            Contract.ThrowIfNull(document);

            using var progress = BufferedProgress.Create<VSInternalReferenceItem>(referenceParams.PartialResultToken);

            var findUsagesService = document.GetRequiredLanguageService<IFindUsagesLSPService>();
            var position = await document.GetPositionFromLinePositionAsync(
                ProtocolConversions.PositionToLinePosition(referenceParams.Position), cancellationToken).ConfigureAwait(false);

            var findUsagesContext = new FindUsagesLSPContext(
                progress, document, position, _metadataAsSourceFileService, _asyncListener, _globalOptions, cancellationToken);

            // Finds the references for the symbol at the specific position in the document, reporting them via streaming to the LSP client.
            await findUsagesService.FindReferencesAsync(findUsagesContext, document, position, cancellationToken).ConfigureAwait(false);
            await findUsagesContext.OnCompletedAsync(cancellationToken).ConfigureAwait(false);

            return progress.GetValues();
        }
    }
}
