// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(InlayHintHandler)), Shared]
    [Method(Methods.TextDocumentInlayHintName)]
    internal sealed class InlayHintHandler : ILspServiceDocumentRequestHandler<InlayHintParams, LSP.InlayHint[]?>
    {
        private readonly IGlobalOptionService _optionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlayHintHandler(IGlobalOptionService optionsService)
        {
            _optionsService = optionsService;
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(InlayHintParams request)
            => request.TextDocument;

        public async Task<LSP.InlayHint[]?> HandleRequestAsync(InlayHintParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(request.Range, text);

            var inlineHintService = document.GetRequiredLanguageService<IInlineHintsService>();
            var options = _optionsService.GetInlineHintsOptions(document.Project.Language);

            var hints = await inlineHintService.GetInlineHintsAsync(document, textSpan, options, displayAllOverride: false, cancellationToken).ConfigureAwait(false);
            if (hints.IsEmpty)
            {
                return Array.Empty<LSP.InlayHint>();
            }

            using var _ = ArrayBuilder<LSP.InlayHint>.GetInstance(out var inlayHints);
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
            var inlayHintCache = context.GetRequiredLspService<InlayHintCache>();

            // Store the members in the resolve cache so that when we get a resolve request for a particular
            // member we can re-use the inline hint.
            var resultId = inlayHintCache.UpdateCache(new InlayHintCache.InlayHintCacheEntry(hints, request.TextDocument, syntaxVersion));

            for (var i = 0; i < hints.Length; i++)
            {
                var hint = hints[i];
                var linePosition = text.Lines.GetLinePosition(hint.Span.Start);
                var kind = hint.Ranking == 0.0
                    ? InlayHintKind.Parameter
                    : InlayHintKind.Type;

                // TextChange is calculated at the same time as the InlineHint,
                // so it should not need to be resolved.
                TextEdit[]? textEdits = null;
                if (hint.ReplacementTextChange.HasValue)
                {
                    var textEdit = ProtocolConversions.TextChangeToTextEdit(hint.ReplacementTextChange.Value, text);
                    textEdits = new TextEdit[] { textEdit };
                }

                var inlayHint = new LSP.InlayHint
                {
                    Position = ProtocolConversions.LinePositionToPosition(linePosition),
                    Label = ConvertTaggedTextToString(hint.DisplayParts),
                    Kind = kind,
                    TextEdits = textEdits,
                    ToolTip = null,
                    PaddingLeft = false,
                    PaddingRight = false,
                    Data = new InlayHintResolveData(resultId, i)
                };

                inlayHints.Add(inlayHint);
            }

            return inlayHints.ToArray();
        }

        private static string ConvertTaggedTextToString(ImmutableArray<TaggedText> displayParts)
        {
            var stringBuilder = new StringBuilder();
            foreach (var displayPart in displayParts)
            {
                stringBuilder.Append(displayPart.Text);
            }

            return stringBuilder.ToString();
        }
    }
}

