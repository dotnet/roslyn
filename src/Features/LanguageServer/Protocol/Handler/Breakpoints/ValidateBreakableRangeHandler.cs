// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(ValidateBreakableRangeHandler)), Shared]
    [Method(LSP.VSInternalMethods.TextDocumentValidateBreakableRangeName)]
    internal sealed class ValidateBreakableRangeHandler : AbstractStatelessRequestHandler<LSP.VSInternalValidateBreakableRangeParams, LSP.Range?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValidateBreakableRangeHandler()
        {
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.VSInternalValidateBreakableRangeParams request)
            => request.TextDocument;

        public override async Task<LSP.Range?> HandleRequestAsync(LSP.VSInternalValidateBreakableRangeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var span = ProtocolConversions.RangeToTextSpan(request.Range, text);
            var breakpointService = document.Project.LanguageServices.GetRequiredService<IBreakpointResolutionService>();

            var result = await breakpointService.ResolveBreakpointAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            // zero-width range means line breakpoint:
            var breakpointSpan = result.IsLineBreakpoint ? new TextSpan(span.Start, length: 0) : result.TextSpan;

            return ProtocolConversions.TextSpanToRange(breakpointSpan, text);
        }
    }
}
