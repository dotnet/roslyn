// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(request.Range, text);

            var inlineHintService = document.GetRequiredLanguageService<IInlineHintsService>();
            var options = _optionsService.GetInlineHintsOptions(document.Project.Language);
            var hints = await inlineHintService.GetInlineHintsAsync(document, textSpan, options, displayAllOverride: false, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<LSP.InlayHint>.GetInstance(hints.Length, out var inlayHints);
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
            var inlayHintCache = context.GetRequiredLspService<InlayHintCache>();

            // Store the members in the resolve cache so that when we get a resolve request for a particular
            // member we can re-use the inline hint.
            var resultId = inlayHintCache.UpdateCache(new InlayHintCache.InlayHintCacheEntry(hints, syntaxVersion));

            for (var i = 0; i < hints.Length; i++)
            {
                var hint = hints[i];
                var (label, leftPadding, rightPadding) = Trim(hint.DisplayParts);
                var linePosition = text.Lines.GetLinePosition(hint.Span.Start);
                var kind = hint.Ranking == InlineHintsConstants.ParameterRanking
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
                    Label = label,
                    Kind = kind,
                    TextEdits = textEdits,
                    ToolTip = null,
                    PaddingLeft = leftPadding,
                    PaddingRight = rightPadding,
                    Data = new InlayHintResolveData(resultId, i, request.TextDocument)
                };

                inlayHints.Add(inlayHint);
            }

            return inlayHints.ToArray();
        }

        /// <summary>
        /// Goes through the tagged text of the hint and trims off leading and trailing spaces. 
        /// If there is leading or trailing space, then we want to add padding to the left and right accordingly.
        /// </summary>
        private static (string label, bool leftPadding, bool rightPadding) Trim(ImmutableArray<TaggedText> taggedTexts)
        {
            using var _ = ArrayBuilder<TaggedText>.GetInstance(out var result);
            var leftPadding = false;
            var rightPadding = false;

            if (taggedTexts.Length == 1)
            {
                var first = taggedTexts.First();

                var trimStart = first.Text.TrimStart();
                var trimBoth = trimStart.TrimEnd();
                result.Add(new TaggedText(first.Tag, trimBoth));
                leftPadding = first.Text.Length - trimStart.Length != 0;
                rightPadding = trimStart.Length - trimBoth.Length != 0;
            }
            else if (taggedTexts.Length >= 2)
            {
                var first = taggedTexts.First();
                var trimStart = first.Text.TrimStart();
                result.Add(new TaggedText(first.Tag, trimStart));
                leftPadding = first.Text.Length - trimStart.Length != 0;

                for (var i = 1; i < taggedTexts.Length - 1; i++)
                    result.Add(taggedTexts[i]);

                var last = taggedTexts.Last();
                var trimEnd = last.Text.TrimEnd();
                result.Add(new TaggedText(last.Tag, trimEnd));
                rightPadding = last.Text.Length - trimEnd.Length != 0;
            }

            return (result.ToImmutable().JoinText(), leftPadding, rightPadding);
        }
    }
}

