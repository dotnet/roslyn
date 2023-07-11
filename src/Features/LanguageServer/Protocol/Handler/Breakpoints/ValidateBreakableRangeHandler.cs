// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(ValidateBreakableRangeHandler)), Shared]
    [Method(LSP.VSInternalMethods.TextDocumentValidateBreakableRangeName)]
    internal sealed class ValidateBreakableRangeHandler : ILspServiceDocumentRequestHandler<VSInternalValidateBreakableRangeParams, LSP.Range?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValidateBreakableRangeHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.VSInternalValidateBreakableRangeParams request)
            => request.TextDocument;

        public async Task<LSP.Range?> HandleRequestAsync(LSP.VSInternalValidateBreakableRangeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var span = ProtocolConversions.RangeToTextSpan(request.Range, text);
            var breakpointService = document.Project.Services.GetRequiredService<IBreakpointResolutionService>();

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

            var breakpointRange = ProtocolConversions.TextSpanToRange(breakpointSpan, text);

            // if the breakpoint we get is smaller than what was requested, then we might be in a situation where
            // the breakpoint was expanded due to the user typing some code above the placement. For example:
            //
            //     $$
            // BP: Console.WriteLine(1);
            //
            // If the user types "int a =" we'll expand the breakpoint, as syntactically its an assigment expression, but then
            // when they continue to type "1;" we'll get a request for a breakpoint that spans two lines, and then the above
            // resolve call will shrink it to one. In that case, we prefer to stick to the end of the requested range.
            //
            // Similar exists for a single line, for example give:
            //
            // BP: int a = $$ GetData();
            //
            // If the user types "1;" we'd shrink the breakpoint, so stick to the end of the range.
            if (!result.IsLineBreakpoint && BreakpointRangeIsSmaller(breakpointRange, request.Range))
            {
                var secondResult = await breakpointService.ResolveBreakpointAsync(document, new TextSpan(span.End, length: 0), cancellationToken).ConfigureAwait(false);
                if (secondResult is not null)
                {
                    breakpointSpan = secondResult.IsLineBreakpoint ? new TextSpan(span.Start, length: 0) : secondResult.TextSpan;
                    breakpointRange = ProtocolConversions.TextSpanToRange(breakpointSpan, text);
                }
            }

            return breakpointRange;
        }

        private static bool BreakpointRangeIsSmaller(LSP.Range breakpointRange, LSP.Range existingRange)
        {
            var breakpointLineDelta = breakpointRange.End.Line - breakpointRange.Start.Line;
            var existingLineDelta = existingRange.End.Line - existingRange.Start.Line;
            return breakpointLineDelta < existingLineDelta ||
                (breakpointLineDelta == existingLineDelta &&
                breakpointRange.End.Character - breakpointRange.Start.Character < existingRange.End.Character - existingRange.Start.Character);
        }
    }
}
