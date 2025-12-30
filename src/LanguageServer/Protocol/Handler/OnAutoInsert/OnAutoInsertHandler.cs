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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.RawStringLiteral;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Completion.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(OnAutoInsertHandler)), Shared]
[Method(LSP.VSInternalMethods.OnAutoInsertName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OnAutoInsertHandler(
    [ImportMany] IEnumerable<Lazy<IBraceCompletionService, LanguageMetadata>> braceCompletionServices,
    IGlobalOptionService globalOptions) : ILspServiceDocumentRequestHandler<LSP.VSInternalDocumentOnAutoInsertParams, LSP.VSInternalDocumentOnAutoInsertResponseItem?>
{
    private readonly ImmutableArray<Lazy<IBraceCompletionService, LanguageMetadata>> _braceCompletionServices = [.. braceCompletionServices];
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.VSInternalDocumentOnAutoInsertParams request) => request.TextDocument;

    public Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> HandleRequestAsync(
        LSP.VSInternalDocumentOnAutoInsertParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document == null)
            return SpecializedTasks.Null<LSP.VSInternalDocumentOnAutoInsertResponseItem>();

        var onAutoInsertEnabled = _globalOptions.GetOption(LspOptionsStorage.LspEnableAutoInsert, document.Project.Language);
        if (!onAutoInsertEnabled)
            return SpecializedTasks.Null<LSP.VSInternalDocumentOnAutoInsertResponseItem>();

        var servicesForDocument = _braceCompletionServices.SelectAsArray(s => s.Metadata.Language == document.Project.Language, s => s.Value);
        var isRazorRequest = context.ServerKind == WellKnownLspServerKinds.RazorLspServer;
        var position = ProtocolConversions.PositionToLinePosition(request.Position);
        var supportsVSExtensions = context.GetRequiredClientCapabilities().HasVisualStudioLspCapability();

        // We want adjust the braces after enter for razor and non-VS clients.
        // We don't do this via on type formatting as it does not support snippets.
        var includeNewLineBraceFormatting = isRazorRequest || !supportsVSExtensions;
        return GetOnAutoInsertResponseAsync(_globalOptions, servicesForDocument, document, position, request.Character, request.Options, includeNewLineBraceFormatting, cancellationToken);
    }

    internal static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> GetOnAutoInsertResponseAsync(
        IGlobalOptionService globalOptions,
        ImmutableArray<IBraceCompletionService> servicesForDocument,
        Document document,
        LinePosition linePosition,
        string character,
        LSP.FormattingOptions lspFormattingOptions,
        bool includeNewLineBraceFormatting,
        CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

        // We should use the options passed in by LSP instead of the document's options.
        var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(lspFormattingOptions, document, cancellationToken).ConfigureAwait(false);

        // The editor calls this handler for C# and VB comment characters, but we only need to process the one for the language that matches the document
        if (character == "\n" || character == service.DocumentationCommentCharacter)
        {
            var docCommentOptions = globalOptions.GetDocumentationCommentOptions(formattingOptions.LineFormatting, document.Project.Language);

            var documentationCommentResponse = await GetDocumentationCommentResponseAsync(
                document, linePosition, character, service, docCommentOptions, cancellationToken).ConfigureAwait(false);

            if (documentationCommentResponse != null)
            {
                return documentationCommentResponse;
            }
        }

        // Only support this for razor as LSP doesn't support overtype yet.
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1165179/
        // Once LSP supports overtype we can move all of brace completion to LSP.

        if (character == "\n" && includeNewLineBraceFormatting)
        {
            var indentationOptions = new IndentationOptions(formattingOptions)
            {
                AutoFormattingOptions = globalOptions.GetAutoFormattingOptions(document.Project.Language)
            };

            var braceCompletionAfterReturnResponse = await GetBraceCompletionAfterReturnResponseAsync(
                document, servicesForDocument, linePosition, indentationOptions, cancellationToken).ConfigureAwait(false);
            if (braceCompletionAfterReturnResponse != null)
            {
                return braceCompletionAfterReturnResponse;
            }
        }

        // Handle raw string literal quote typing
        if (character == "\"")
        {
            var rawStringResponse = await GetRawStringLiteralResponseAsync(document, linePosition, cancellationToken).ConfigureAwait(false);
            if (rawStringResponse != null)
            {
                return rawStringResponse;
            }
        }

        return null;
    }

    private static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> GetDocumentationCommentResponseAsync(
        Document document,
        LinePosition linePosition,
        string character,
        IDocumentationCommentSnippetService service,
        DocumentationCommentOptions options,
        CancellationToken cancellationToken)
    {
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var sourceText = parsedDocument.Text;

        var position = sourceText.Lines.GetPosition(linePosition);

        var result = character == "\n"
            ? service.GetDocumentationCommentSnippetOnEnterTyped(parsedDocument, position, options, cancellationToken)
            : service.GetDocumentationCommentSnippetOnCharacterTyped(parsedDocument, position, options, cancellationToken);

        if (result == null)
            return null;

        return new LSP.VSInternalDocumentOnAutoInsertResponseItem
        {
            TextEditFormat = LSP.InsertTextFormat.Snippet,
            TextEdit = new LSP.TextEdit
            {
                NewText = result.SnippetText.Insert(result.CaretOffset, "$0"),
                Range = ProtocolConversions.TextSpanToRange(result.SpanToReplace, sourceText)
            }
        };
    }

    private static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> GetBraceCompletionAfterReturnResponseAsync(
        Document document,
        ImmutableArray<IBraceCompletionService> servicesForDocument,
        LinePosition linePosition,
        IndentationOptions options,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var position = sourceText.Lines.GetPosition(linePosition);

        var serviceAndContext = await GetBraceCompletionContextAsync(servicesForDocument, position, document, cancellationToken).ConfigureAwait(false);
        if (serviceAndContext == null)
        {
            return null;
        }

        var (service, context) = serviceAndContext.Value;
        var postReturnEdit = service.GetTextChangeAfterReturn(context, options, cancellationToken);
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
                var indentedText = GetIndentedText(newSourceText, caretLine, desiredCaretLinePosition, options);

                // Get the overall text changes between the original text and the formatted + indented text.
                textChanges = [.. indentedText.GetTextChanges(sourceText)];
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

        var autoInsertChange = new LSP.VSInternalDocumentOnAutoInsertResponseItem
        {
            TextEditFormat = LSP.InsertTextFormat.Snippet,
            TextEdit = new LSP.TextEdit
            {
                NewText = newText,
                Range = ProtocolConversions.TextSpanToRange(textChange.Span, sourceText)
            }
        };

        return autoInsertChange;

        static SourceText GetIndentedText(
            SourceText textToIndent,
            TextLine lineToIndent,
            LinePosition desiredCaretLinePosition,
            IndentationOptions options)
        {
            // Indent by the amount needed to make the caret line contain the desired indentation column.
            var amountToIndent = desiredCaretLinePosition.Character - lineToIndent.Span.Length;

            // Create and apply a text change with whitespace for the indentation amount.
            var indentText = amountToIndent.CreateIndentationString(options.FormattingOptions.UseTabs, options.FormattingOptions.TabSize);
            var indentedText = textToIndent.WithChanges(new TextChange(new TextSpan(lineToIndent.End, 0), indentText));
            return indentedText;
        }

        static async Task<TextChange> GetCollapsedChangeAsync(ImmutableArray<TextChange> textChanges, Document oldDocument, CancellationToken cancellationToken)
        {
            var documentText = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
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

    private static async Task<(IBraceCompletionService Service, BraceCompletionContext Context)?> GetBraceCompletionContextAsync(ImmutableArray<IBraceCompletionService> servicesForDocument, int caretLocation, Document document, CancellationToken cancellationToken)
    {
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var fallbackOptions = document.Project.GetFallbackAnalyzerOptions();

        foreach (var service in servicesForDocument)
        {
            var context = service.GetCompletedBraceContext(parsedDocument, fallbackOptions, caretLocation);
            if (context != null)
            {
                return (service, context.Value);
            }
        }

        return null;
    }

    private static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> GetRawStringLiteralResponseAsync(
        Document document,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        var service = document.GetLanguageService<IRawStringLiteralAutoInsertService>();
        if (service == null)
            return null;

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var originalPosition = sourceText.Lines.GetPosition(linePosition);

        // The service expects to receive a document without the typed quote and position where the quote would be typed.
        // We were passed a document with the quote already inserted and position after the quote.  Hence we need to
        // adjust the document backwards to remove the quote and move the position back by one.
        var positionOfQuote = originalPosition - 1;
        var sourceTextWithoutQuote = sourceText.WithChanges(new TextChange(new TextSpan(positionOfQuote, 1), string.Empty));
        var documentWithoutQuote = document.WithText(sourceTextWithoutQuote);

        var textChange = service.GetTextChangeForQuote(documentWithoutQuote, sourceTextWithoutQuote, positionOfQuote, cancellationToken);
        if (textChange == null)
            return null;

        // The server returns an edit to be applied after the quote has already been typed.
        // The original request is based on the document with the quote already inserted, so we can just return the edit
        // directly against the request document.
        var edit = ProtocolConversions.TextChangeToTextEdit(textChange.Value, sourceText);
        var format = LSP.InsertTextFormat.Plaintext;

        if (textChange.Value.Span.Start == originalPosition)
        {
            // The raw string edit may start with the original position (i.e. the caret position).
            // In such a case, we need to return a snippet edit to ensure the caret is not moved to the end.
            edit.NewText = edit.NewText.Insert(0, "$0"); // Insert caret at the original position after the typed quote.
            format = LSP.InsertTextFormat.Snippet;
        }

        return new LSP.VSInternalDocumentOnAutoInsertResponseItem
        {
            TextEditFormat = format,
            TextEdit = edit
        };
    }
}
