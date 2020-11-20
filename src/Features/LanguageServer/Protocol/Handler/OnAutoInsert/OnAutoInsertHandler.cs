// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.Completion.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.MSLSPMethods.OnAutoInsertName, mutatesSolutionState: false)]
    internal class OnAutoInsertHandler : IRequestHandler<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem?>
    {
        private readonly ImmutableArray<IBraceCompletionService> _csharpBraceCompletionServices;
        private readonly ImmutableArray<IBraceCompletionService> _visualBasicBraceCompletionServices;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnAutoInsertHandler(
            [ImportMany(LanguageNames.CSharp)] IEnumerable<IBraceCompletionService> csharpBraceCompletionServices,
            [ImportMany(LanguageNames.VisualBasic)] IEnumerable<IBraceCompletionService> visualBasicBraceCompletionServices)
        {
            _csharpBraceCompletionServices = csharpBraceCompletionServices.ToImmutableArray();
            _visualBasicBraceCompletionServices = _visualBasicBraceCompletionServices.ToImmutableArray();
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DocumentOnAutoInsertParams request) => request.TextDocument;

        public async Task<LSP.DocumentOnAutoInsertResponseItem?> HandleRequestAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;

            if (document == null)
            {
                return null;
            }

            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

            // The editor calls this handler for C# and VB comment characters, but we only need to process the one for the language that matches the document
            if (autoInsertParams.Character == "\n" || autoInsertParams.Character == service.DocumentationCommentCharacter)
            {
                var documentationCommentResponse = await GetDocumentationCommentResponseAsync(autoInsertParams, document, service, cancellationToken).ConfigureAwait(false);
                if (documentationCommentResponse != null)
                {
                    return documentationCommentResponse;
                }
            }

            // Only support this for razor as LSP doesn't support overtype yet.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1165179/
            // Once LSP supports overtype we can move all of brace completion to LSP.
            if (autoInsertParams.Character == "\n" && context.ClientName == document.Services.GetService<DocumentPropertiesService>()?.DiagnosticsLspClientName)
            {
                var braceCompletionAfterReturnResponse = await GetBraceCompletionAfterReturnResponseAsync(autoInsertParams, document, cancellationToken).ConfigureAwait(false);
                if (braceCompletionAfterReturnResponse != null)
                {
                    return braceCompletionAfterReturnResponse;
                }
            }

            return null;
        }

        private static async Task<LSP.DocumentOnAutoInsertResponseItem?> GetDocumentationCommentResponseAsync(
            LSP.DocumentOnAutoInsertParams autoInsertParams,
            Document document,
            IDocumentationCommentSnippetService service,
            CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var linePosition = ProtocolConversions.PositionToLinePosition(autoInsertParams.Position);
            var position = sourceText.Lines.GetPosition(linePosition);

            var result = autoInsertParams.Character == "\n"
                ? service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, sourceText, position, options, cancellationToken)
                : service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, sourceText, position, options, cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new LSP.DocumentOnAutoInsertResponseItem
            {
                TextEditFormat = LSP.InsertTextFormat.Snippet,
                TextEdit = new LSP.TextEdit
                {
                    NewText = result.SnippetText.Insert(result.CaretOffset, "$0"),
                    Range = ProtocolConversions.TextSpanToRange(result.SpanToReplace, sourceText)
                }
            };
        }

        private async Task<LSP.DocumentOnAutoInsertResponseItem?> GetBraceCompletionAfterReturnResponseAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, Document document, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var position = sourceText.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(autoInsertParams.Position));

            var serviceAndContext = await GetBraceCompletionContextAsync(position, document, cancellationToken).ConfigureAwait(false);
            if (serviceAndContext == null)
            {
                return null;
            }

            var (service, context) = serviceAndContext.Value;
            var postReturnEdit = await service.GetTextChangeAfterReturnAsync(context, cancellationToken).ConfigureAwait(false);
            if (postReturnEdit == null)
            {
                return null;
            }

            var textChanges = postReturnEdit.Value.TextChanges;
            var desiredCaretLinePosition = postReturnEdit.Value.CaretLocation;
            var newSourceText = sourceText.WithChanges(textChanges);

            var caretLine = newSourceText.Lines[desiredCaretLinePosition.Line];
            if (desiredCaretLinePosition.Character > caretLine.Span.Length)
            {
                if (caretLine.Span.IsEmpty)
                {
                    // We have an empty line with the caret column at an indented position, let's add whitespace indentation to the text.
                    var indentedText = await GetIndentedTextAsync(document, newSourceText, caretLine, desiredCaretLinePosition, cancellationToken).ConfigureAwait(false);

                    // Get the overall text changes between the original text and the formatted + indented text.
                    textChanges = indentedText.GetTextChanges(sourceText).ToImmutableArray();
                    newSourceText = indentedText;

                    // If tabs were inserted the desired caret column can remain beyond the line text.
                    // So just set the caret position to the end of the newly indented line.
                    var caretLineInIndentedText = indentedText.Lines[desiredCaretLinePosition.Line];
                    desiredCaretLinePosition = indentedText.Lines.GetLinePosition(caretLineInIndentedText.End);
                }
                else
                {
                    // We're not on an empty line, clamp the line position to the actual line end.
                    desiredCaretLinePosition = new LinePosition(desiredCaretLinePosition.Line, Math.Min(desiredCaretLinePosition.Character, caretLine.End));
                }
            }

            var textChange = await GetCollapsedChangeAsync(textChanges, document, cancellationToken).ConfigureAwait(false);
            var newText = GetTextChangeTextWithCaretAtLocation(newSourceText, textChange, desiredCaretLinePosition);
            var autoInsertChange = new LSP.DocumentOnAutoInsertResponseItem
            {
                TextEditFormat = LSP.InsertTextFormat.Snippet,
                TextEdit = new LSP.TextEdit
                {
                    NewText = newText,
                    Range = ProtocolConversions.TextSpanToRange(textChange.Span, sourceText)
                }
            };

            return autoInsertChange;

            static async Task<SourceText> GetIndentedTextAsync(Document originalDocument, SourceText textToIndent, TextLine lineToIndent, LinePosition desiredCaretLinePosition, CancellationToken cancellationToken)
            {
                // Indent by the amount needed to make the caret line contain the desired indentation column.
                var amountToIndent = desiredCaretLinePosition.Character - lineToIndent.Span.Length;

                // Create and apply a text change with whitespace for the indentation amount.
                var documentOptions = await originalDocument.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var indentText = amountToIndent.CreateIndentationString(documentOptions.GetOption(FormattingOptions.UseTabs), documentOptions.GetOption(FormattingOptions.TabSize));
                var indentedText = textToIndent.WithChanges(new TextChange(new TextSpan(lineToIndent.End, 0), indentText));
                return indentedText;
            }

            static async Task<TextChange> GetCollapsedChangeAsync(ImmutableArray<TextChange> textChanges, Document oldDocument, CancellationToken cancellationToken)
            {
                var documentText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                documentText = documentText.WithChanges(textChanges);
                return Collapse(documentText, textChanges);
            }

            static string GetTextChangeTextWithCaretAtLocation(SourceText sourceText, TextChange textChange, LinePosition desiredCaretLinePosition)
            {
                var desiredCaretLocation = sourceText.Lines.GetPosition(desiredCaretLinePosition);
                Debug.Assert(desiredCaretLocation >= textChange.Span.Start);
                var offsetInTextChange = desiredCaretLocation - textChange.Span.Start;
                var newText = textChange.NewText!.Insert(offsetInTextChange, "$0");
                return newText;
            }
        }

        private async Task<(IBraceCompletionService Service, BraceCompletionContext Context)?> GetBraceCompletionContextAsync(int caretLocation, Document document, CancellationToken cancellationToken)
        {
            var servicesForDocument = document.Project.Language switch
            {
                LanguageNames.CSharp => _csharpBraceCompletionServices,
                LanguageNames.VisualBasic => _visualBasicBraceCompletionServices,
                _ => throw new ArgumentException($"Language {document.Project.Language} is not recognized for OnAutoInsert")
            };

            foreach (var service in servicesForDocument)
            {
                var context = await service.GetCompletedBraceContextAsync(document, caretLocation, cancellationToken).ConfigureAwait(false);
                if (context != null)
                {
                    return (service, context.Value);
                }
            }

            return null;
        }
    }
}
