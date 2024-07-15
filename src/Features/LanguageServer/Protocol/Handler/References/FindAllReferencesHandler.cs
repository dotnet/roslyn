// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(FindAllReferencesHandler)), Shared]
    [Method(LSP.Methods.TextDocumentReferencesName)]
    internal sealed class FindAllReferencesHandler : ILspServiceDocumentRequestHandler<LSP.ReferenceParams, LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]?>
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

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(ReferenceParams request) => request.TextDocument;

        public async Task<LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]?> HandleRequestAsync(
            ReferenceParams referenceParams,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            var document = context.Document;
            var workspace = context.Workspace;
            Contract.ThrowIfNull(document);
            Contract.ThrowIfNull(workspace);

            using var progress = BufferedProgress.Create<SumType<VSInternalReferenceItem, LSP.Location>[]>(referenceParams.PartialResultToken);

            var findUsagesService = document.GetRequiredLanguageService<IFindUsagesLSPService>();
            var position = await document.GetPositionFromLinePositionAsync(
                ProtocolConversions.PositionToLinePosition(referenceParams.Position), cancellationToken).ConfigureAwait(false);

            var findUsagesContext = new FindUsagesLSPContext(
                progress, workspace, document, position, _metadataAsSourceFileService, _asyncListener, _globalOptions, cancellationToken);

            // Finds the references for the symbol at the specific position in the document, reporting them via streaming to the LSP client.
            var classificationOptions = _globalOptions.GetClassificationOptionsProvider();
            await findUsagesService.FindReferencesAsync(findUsagesContext, document, position, classificationOptions, cancellationToken).ConfigureAwait(false);
            await findUsagesContext.OnCompletedAsync(cancellationToken).ConfigureAwait(false);

            return progress.GetFlattenedValues();
        }
    }
}
