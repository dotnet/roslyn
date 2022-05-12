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
using System.Linq;

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

            if (span.Length > 0)
            {
                // If we have a non-empty span then it means that the debugger is asking us to adjust an
                // existing span.  In Everett we didn't do this so we had some good and some bad
                // behavior.  For example if you had a breakpoint on: "int i = 1;" and you changed it to "int
                // i = 1, j = 2;", then the breakpoint wouldn't adjust.  That was bad.  However, if you had the
                // breakpoint on an open or close curly brace then it would always "stick" to that brace
                // which was good.
                //
                // So we want to keep the best parts of both systems.  We want to appropriately "stick"
                // to tokens and we also want to adjust spans intelligently.
                //
                // However, it turns out the latter is hard to do when there are parse errors in the
                // code.  Things like missing name nodes cause a lot of havoc and make it difficult to
                // track a closing curly brace.
                //
                // So the way we do this is that we default to not intelligently adjusting the spans
                // while there are parse errors.  But when there are no parse errors then the span is
                // adjusted.
                if (document.SupportsSyntaxTree)
                {
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(tree);
                    if (tree.GetDiagnostics(cancellationToken).Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        // Keep the span as is.
                        return request.Range;
                    }
                }
            }

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
