// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions.XmlSnippetParser;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

/// <summary>
/// Supports built in legacy snippets for razor scenarios.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(InlineCompletionsHandler)), Shared]
[Method(VSInternalMethods.TextDocumentInlineCompletionName)]
internal partial class InlineCompletionsHandler : ILspServiceDocumentRequestHandler<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList?>
{
    /// <summary>
    /// The set of built in snippets from, typically found in
    /// C:\Program Files\Microsoft Visual Studio\2022\VS_INSTANCE\VC#\Snippets\1033\Visual C#
    /// These are currently the only snippets supported.
    /// </summary>
    public static ImmutableHashSet<string> BuiltInSnippets = ImmutableHashSet.Create(
        "~", "Attribute", "checked", "class", "ctor", "cw", "do", "else", "enum", "equals", "Exception", "for", "foreach", "forr",
        "if", "indexer", "interface", "invoke", "iterator", "iterindex", "lock", "mbox", "namespace", "#if", "#region", "prop",
        "propfull", "propg", "sim", "struct", "svm", "switch", "try", "tryf", "unchecked", "unsafe", "using", "while");

    private readonly XmlSnippetParser _xmlSnippetParser;
    private readonly IGlobalOptionService _globalOptions;

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlineCompletionsHandler(XmlSnippetParser xmlSnippetParser, IGlobalOptionService globalOptions)
    {
        _xmlSnippetParser = xmlSnippetParser;
        _globalOptions = globalOptions;
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalInlineCompletionRequest request)
    {
        return request.TextDocument;
    }

    public async Task<VSInternalInlineCompletionList?> HandleRequestAsync(VSInternalInlineCompletionRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();

        // First get available snippets if any.
        var snippetInfoService = document.Project.GetRequiredLanguageService<ISnippetInfoService>();
        var snippetInfo = snippetInfoService.GetSnippetsIfAvailable();
        if (!snippetInfo.Any())
        {
            return null;
        }

        // Then attempt to get the word at the requested position.
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFactsService = document.Project.GetRequiredLanguageService<ISyntaxFactsService>();
        var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
        var position = sourceText.Lines.GetPosition(linePosition);
        if (!SnippetUtilities.TryGetWordOnLeft(position, sourceText, syntaxFactsService, out var wordOnLeft))
        {
            return null;
        }

        // Find the snippet with shortcut text that matches the typed word.
        var wordText = sourceText.GetSubText(wordOnLeft.Value).ToString();
        if (!BuiltInSnippets.Contains(wordText, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var matchingSnippetInfo = snippetInfo.First(s => wordText.Equals(s.Shortcut, StringComparison.OrdinalIgnoreCase));

        var parsedSnippet = _xmlSnippetParser.GetParsedXmlSnippet(matchingSnippetInfo, context);
        if (parsedSnippet == null)
        {
            return null;
        }

        // Use the formatting options specified by the client to format the snippet.
        var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(request.Options, document, _globalOptions, cancellationToken).ConfigureAwait(false);
        var simplifierOptions = await document.GetSimplifierOptionsAsync(_globalOptions, cancellationToken).ConfigureAwait(false);

        var formattedLspSnippet = await GetFormattedLspSnippetAsync(parsedSnippet, wordOnLeft.Value, document, sourceText, formattingOptions, simplifierOptions, cancellationToken).ConfigureAwait(false);

        return new VSInternalInlineCompletionList
        {
            Items =
            [
                new VSInternalInlineCompletionItem
                {
                    Range = ProtocolConversions.TextSpanToRange(wordOnLeft.Value, sourceText),
                    Text = formattedLspSnippet,
                    TextFormat = InsertTextFormat.Snippet,
                }
            ]
        };
    }

    /// <summary>
    /// Formats the snippet by applying the snippet to the document with the default values / function results for snippet declarations.
    /// Then converts back into an LSP snippet by replacing the declarations with the appropriate LSP tab stops.
    /// 
    /// Note that the operations in this method are sensitive to the context in the document and so must be calculated on each request.
    /// </summary>
    private static async Task<string> GetFormattedLspSnippetAsync(
        ParsedXmlSnippet parsedSnippet,
        TextSpan snippetShortcut,
        Document originalDocument,
        SourceText originalSourceText,
        SyntaxFormattingOptions formattingOptions,
        SimplifierOptions simplifierOptions,
        CancellationToken cancellationToken)
    {
        // Calculate the snippet text with defaults + snippet function results.
        var (snippetFullText, fields, caretSpan) = await GetReplacedSnippetTextAsync(
            originalDocument, originalSourceText, snippetShortcut, parsedSnippet, simplifierOptions, cancellationToken).ConfigureAwait(false);

        // Create a document with the default snippet text that we can use to format the snippet.
        var textChange = new TextChange(snippetShortcut, snippetFullText);
        var snippetEndPosition = textChange.Span.Start + textChange.NewText!.Length;

        var documentWithSnippetText = originalSourceText.WithChanges(textChange);
        var root = await originalDocument.WithText(documentWithSnippetText).GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var spanToFormat = TextSpan.FromBounds(textChange.Span.Start, snippetEndPosition);
        var formattingChanges = Formatter.GetFormattedTextChanges(root, spanToFormat, originalDocument.Project.Solution.Services, formattingOptions, cancellationToken: cancellationToken)
            ?.ToImmutableArray() ?? ImmutableArray<TextChange>.Empty;

        var formattedText = documentWithSnippetText.WithChanges(formattingChanges);

        // We now have a formatted snippet with default values.  We need to
        // replace the fields and caret with the proper LSP tab stop notation.
        // Since formatting changes are entirely whitespace, we can calculate the new locations by
        // adjusting the old spans based on the formatting changes that occured before them.

        // Get the adjusted snippet bounds.
        snippetEndPosition = GetAdjustedSpan(formattingChanges, new TextSpan(snippetEndPosition, 0)).Start;
        var spanContainingFormattedSnippet = TextSpan.FromBounds(snippetShortcut.Start, snippetEndPosition);

        // Get the adjusted fields and determine the text edits to make LSP formatted tab stops.
        using var _1 = ArrayBuilder<TextChange>.GetInstance(out var lspTextChanges);
        foreach (var (field, spans) in fields)
        {
            var lspTextForField = string.IsNullOrEmpty(field.DefaultText) ? $"${{{field.EditIndex}}}" : $"${{{field.EditIndex}:{field.DefaultText}}}";
            foreach (var span in spans)
            {
                // Adjust the span based on the formatting changes and build the snippet text change.
                var fieldInFormattedText = GetAdjustedSpan(formattingChanges, span);
                var fieldInSnippetContext = GetTextSpanInContextOfSnippet(fieldInFormattedText.Start, spanContainingFormattedSnippet.Start, fieldInFormattedText.Length);
                lspTextChanges.Add(new TextChange(fieldInSnippetContext, lspTextForField));
            }
        }

        // Get the adjusted caret location and replace the placeholder comment with the LSP formatted tab stop.
        if (caretSpan != null)
        {
            var caretInFormattedText = GetAdjustedSpan(formattingChanges, caretSpan.Value);
            var caretInSnippetContext = GetTextSpanInContextOfSnippet(caretInFormattedText.Start, spanContainingFormattedSnippet.Start, caretInFormattedText.Length);
            lspTextChanges.Add(new TextChange(caretInSnippetContext, "$0"));
        }

        // Apply all the text changes to get the text formatted as the LSP snippet syntax.
        var formattedLspSnippetText = formattedText.GetSubText(spanContainingFormattedSnippet).WithChanges(lspTextChanges);

        return formattedLspSnippetText.ToString();

        static TextSpan GetAdjustedSpan(ImmutableArray<TextChange> textChanges, TextSpan originalSpan)
        {
            var textChangesBefore = textChanges.Where(t => t.Span.End <= originalSpan.Start);
            var amountToAdjust = textChangesBefore.Sum(t => t.NewText!.Length - t.Span.Length);
            return new TextSpan(originalSpan.Start + amountToAdjust, originalSpan.Length);
        }

        static TextSpan GetTextSpanInContextOfSnippet(int positionInFullText, int snippetPositionInFullText, int length)
        {
            var offsetInSnippet = new TextSpan(positionInFullText - snippetPositionInFullText, length);
            return offsetInSnippet;
        }
    }

    /// <summary>
    /// Create the snippet with the full default text and functions applied.  Output the spans associated with
    /// each field and the final caret location in that text so that we can find those locations later.
    /// </summary>
    private static async Task<(string ReplacedSnippetText, ImmutableDictionary<SnippetFieldPart, ImmutableArray<TextSpan>> Fields, TextSpan? CaretSpan)> GetReplacedSnippetTextAsync(
        Document originalDocument,
        SourceText originalSourceText,
        TextSpan snippetSpan,
        ParsedXmlSnippet parsedSnippet,
        SimplifierOptions simplifierOptions,
        CancellationToken cancellationToken)
    {
        var documentWithDefaultSnippet = originalDocument.WithText(
            originalSourceText.WithChanges(new TextChange(snippetSpan, parsedSnippet.DefaultText)));

        // Iterate the snippet parts so that we can do two things:
        //   1.  Calculate the snippet function result.  This must be done against the document containing the default snippet text
        //       as the function result is context dependent.
        //   2.  After inserting the function result, determine the spans associated with each editable snippet field.
        var fieldOffsets = new Dictionary<SnippetFieldPart, ImmutableArray<TextSpan>>();
        using var _ = PooledStringBuilder.GetInstance(out var functionSnippetBuilder);
        TextSpan? caretSpan = null;

        // This represents the field start location in the context of the snippet without functions replaced (see below).
        var locationInDefaultSnippet = snippetSpan.Start;

        // This represents the field start location in the context of the snippet with functions replaced.
        var locationInFinalSnippet = snippetSpan.Start;
        foreach (var originalPart in parsedSnippet.Parts)
        {
            var part = originalPart;

            // Adjust the text associated with this snippet part by applying the result of the snippet function.
            if (part is SnippetFunctionPart functionPart)
            {
                // To avoid a bunch of document changes and re-parsing, we always calculate the snippet function result
                // against the document with the default snippet text applied to it instead of with each incremental function result.
                // So we need to remember the index into the original document.
                part = await functionPart.WithSnippetFunctionResultAsync(documentWithDefaultSnippet, new TextSpan(locationInDefaultSnippet, part.DefaultText.Length), simplifierOptions, cancellationToken).ConfigureAwait(false);
            }

            // Only store spans for editable fields or the cursor location, we don't need to get back to anything else.
            if (part is SnippetFieldPart fieldPart && fieldPart.EditIndex != null)
            {
                var fieldSpan = new TextSpan(locationInFinalSnippet, part.DefaultText.Length);
                fieldOffsets[fieldPart] = fieldOffsets.GetValueOrDefault(fieldPart, ImmutableArray<TextSpan>.Empty).Add(fieldSpan);
            }
            else if (part is SnippetCursorPart cursorPart)
            {
                caretSpan = new TextSpan(locationInFinalSnippet, cursorPart.DefaultText.Length);
            }

            // Append the new snippet part to the text and track the location of the field in the text w/ functions.
            locationInFinalSnippet += part.DefaultText.Length;
            functionSnippetBuilder.Append(part.DefaultText);

            // Keep track of the original field location in the text w/out functions.
            locationInDefaultSnippet += originalPart.DefaultText.Length;
        }

        return (functionSnippetBuilder.ToString(), fieldOffsets.ToImmutableDictionary(), caretSpan);
    }
}
