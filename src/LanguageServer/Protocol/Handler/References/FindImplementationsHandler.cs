// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(FindImplementationsHandler)), Shared]
    [Method(LSP.Methods.TextDocumentImplementationName)]
    internal sealed class FindImplementationsHandler : ILspServiceDocumentRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindImplementationsHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;

        public Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var supportsVisualStudioExtensions = context.GetRequiredClientCapabilities().HasVisualStudioLspCapability();
            var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
            var classificationOptions = _globalOptions.GetClassificationOptionsProvider();

            return FindImplementationsAsync(document, linePosition, classificationOptions, supportsVisualStudioExtensions, cancellationToken);
        }

        internal static async Task<LSP.Location[]> FindImplementationsAsync(Document document, LinePosition linePosition, OptionsProvider<ClassificationOptions> classificationOptions, bool supportsVisualStudioExtensions, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var findUsagesService = document.GetRequiredLanguageService<IFindUsagesLSPService>();
            var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

            var findUsagesContext = new SimpleFindUsagesContext();
            await findUsagesService.FindImplementationsAsync(findUsagesContext, document, position, classificationOptions, cancellationToken).ConfigureAwait(false);

            foreach (var definition in findUsagesContext.GetDefinitions())
            {
                var text = definition.GetClassifiedText();
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    if (supportsVisualStudioExtensions)
                    {
                        locations.AddIfNotNull(await ProtocolConversions.DocumentSpanToLocationWithTextAsync(sourceSpan, text, cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        locations.AddIfNotNull(await ProtocolConversions.DocumentSpanToLocationAsync(sourceSpan, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            return locations.ToArrayAndFree();
        }
    }
}
