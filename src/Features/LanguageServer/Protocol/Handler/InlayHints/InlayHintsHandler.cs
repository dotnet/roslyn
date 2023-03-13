// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHints
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(InlayHintsHandler)), Shared]
    [Method(LSP.Methods.TextDocumentInlayHint)]
    internal sealed class InlayHintsHandler : ILspServiceDocumentRequestHandler<LSP.InlayHintParams, LSP.InlayHint[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlayHintsHandler()
        {
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(InlayHintParams request)
            => request.TextDocument;

        public async Task<InlayHint[]?> HandleRequestAsync(InlayHintParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(request.Range, text);

            using var _ = ArrayBuilder<LSP.InlayHint>.GetInstance(out var inlayHints);
            await AddParameterHintsToBuilderAsync(document, text, textSpan, inlayHints, cancellationToken).ConfigureAwait(false);
            //await AddTypeHintsToBuilderAsync(document, textSpan, inlayHints, cancellationToken).ConfigureAwait(false);
            return inlayHints.ToArray();
        }

        private static async Task AddParameterHintsToBuilderAsync(Document document, SourceText text, TextSpan textSpan, ArrayBuilder<LSP.InlayHint> inlayHints, CancellationToken cancellationToken)
        {
            var inlineParameterService = document.GetRequiredLanguageService<IInlineParameterNameHintsService>();
            var options = new InlineParameterHintsOptions()
            {
                EnabledForParameters = true
            };

            var displayOptions = new SymbolDescriptionOptions();
            var parameterHints = await inlineParameterService.GetInlineHintsAsync(document, textSpan, options, displayOptions, cancellationToken).ConfigureAwait(false);

            foreach (var parameterHint in parameterHints)
            {
                var linePosition = text.Lines.GetLinePosition(parameterHint.Span.Start);
                LSP.TextEdit[]? textEdits = null;
                if (parameterHint.ReplacementTextChange.HasValue)
                {
                    var textEdit = ProtocolConversions.TextChangeToTextEdit(parameterHint.ReplacementTextChange.Value, text);
                    textEdits = new LSP.TextEdit[] { textEdit };
                }

                var inlayHint = new LSP.InlayHint
                {
                    Position = ProtocolConversions.LinePositionToPosition(linePosition),
                    Label = "test",
                    Kind = LSP.InlayHintKind.Parameter,
                    TextEdits = textEdits,
                    ToolTip = null,
                    PaddingLeft = false,
                    PaddingRight = false
                };

                inlayHints.Add(inlayHint);
            }
        }

        /*private async Task AddTypeHintsToBuilderAsync(Document document, TextSpan textSpan, ArrayBuilder<InlayHint> inlayHints, CancellationToken cancellationToken)
        {
            var inlineTypeService = document.GetRequiredLanguageService<IInlineTypeHintsService>();
            var options = new InlineTypeHintsOptions()
            {
                EnabledForTypes = true
            };

            var displayOptions = new SymbolDescriptionOptions();
            var typeHints = await inlineTypeService.GetInlineHintsAsync(document, textSpan, options, displayOptions, cancellationToken).ConfigureAwait(false);

            foreach (var typeHint in typeHints)
            {
                var inlayHint = new LSP.InlayHint
                {

                };
            }
        }*/
    }
}
