// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

/// <summary>
/// Supports built in legacy snippets for razor scenarios.
/// </summary>
[ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
[ProvidesMethod(VSInternalMethods.TextDocumentInlineCompletionName)]
internal partial class InlineCompletionsHandler : AbstractStatelessRequestHandler<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList>
{
    public static ImmutableArray<string> BuiltInSnippets = ImmutableArray.Create(
        "~", "Attribute", "checked", "class", "ctor", "cw", "do", "else", "enum", "equals", "Exception", "for", "foreach", "forr",
        "if", "indexer", "interface", "invoke", "iterator", "iterindex", "lock", "mbox", "namespace", "#if", "#region", "prop",
        "propfull", "propg", "sim", "struct", "svm", "switch", "try", "tryf", "unchecked", "unsafe", "using", "while");

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlineCompletionsHandler()
    {
    }

    public override string Method => VSInternalMethods.TextDocumentInlineCompletionName;

    public override bool MutatesSolutionState => false;

    public override bool RequiresLSPSolution => true;

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalInlineCompletionRequest request)
    {
        return request.TextDocument;
    }

    public override async Task<VSInternalInlineCompletionList> HandleRequestAsync(VSInternalInlineCompletionRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Document);

        // First get available snippets if any.
        var snippetInfoService = context.Document.Project.GetRequiredLanguageService<ISnippetInfoService>();
        var snippetInfo = snippetInfoService.GetSnippetsIfAvailable();
        if (!snippetInfo.Any())
        {
            return new VSInternalInlineCompletionList();
        }

        // Then attempt to get the word at the requested position.
        var sourceText = await context.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFactsService = context.Document.Project.GetRequiredLanguageService<ISyntaxFactsService>();
        var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
        var position = sourceText.Lines.GetPosition(linePosition);
        if (!TryGetWordOnLeft(position, sourceText, syntaxFactsService, out var wordOnLeft))
        {
            return new VSInternalInlineCompletionList();
        }

        // Find the snippet with shortcut text that matches the typed word.
        var wordText = sourceText.GetSubText(wordOnLeft.Value).ToString();
        var matchingSnippetInfo = snippetInfo.Single(s => wordText.Equals(s.Shortcut, StringComparison.OrdinalIgnoreCase));

        // Convert xml snippets
        // Include function calls (see SnippetExpansionClient) to get switch/class name
        // loc - C:\Program Files\Microsoft Visual Studio\2022\Main\VC#\Snippets\1033\Visual C#

        var matchingSnippet = RetrieveSnippetFromXml(wordText, matchingSnippetInfo, context);
        if (matchingSnippet == null)
        {
            return new VSInternalInlineCompletionList();
        }

        // We currently only support snippet expansions, the others require selection support which is N/A here.
        if (!matchingSnippet.SnippetTypes.Contains("Expansion", StringComparer.OrdinalIgnoreCase))
        {
            return new VSInternalInlineCompletionList();
        }

        var expansion = new ExpansionTemplate(matchingSnippet);

        // Get snippet with replaced values
        var snippetFullText = expansion.GetCodeSnippet();
        if (snippetFullText == null)
        {
            return new VSInternalInlineCompletionList();
        }

        var formattedLspSnippet = await GetFormattedLspSnippetAsync(snippetFullText, expansion, wordOnLeft.Value, context.Document, sourceText, cancellationToken).ConfigureAwait(false);

        return new VSInternalInlineCompletionList
        {
            Items = new VSInternalInlineCompletionItem[]
            {
                new VSInternalInlineCompletionItem
                {
                    Range = ProtocolConversions.TextSpanToRange(wordOnLeft.Value, sourceText),
                    Text = formattedLspSnippet,
                    TextFormat = InsertTextFormat.Snippet,
                }
            }
        };
    }

    /// <summary>
    /// Formats the snippet by applying the snippet to the document with the default values for declarations.
    /// Then converts back into an LSP snippet by replacing the declarations with the appropriate LSP tab stops.
    /// </summary>
    private static async Task<string> GetFormattedLspSnippetAsync(string snippetFullText, ExpansionTemplate snippetExpansion, TextSpan snippetShortcut, Document originalDocument, SourceText originalSourceText, CancellationToken cancellationToken)
    {
        // Create a document with the default snippet text that we can use to format the snippet.
        var textChange = new TextChange(snippetShortcut, snippetFullText);
        var documentWithSnippet = originalDocument.WithText(originalSourceText.WithChanges(textChange));
        var root = await documentWithSnippet.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Attach annotations to the start and end of the snippet insertion so that we can extract it out after formatting.
        root = AnnotateSnippetBounds(root, textChange, out var startAnnotation, out var endAnnotation);

        // Attach annotations to the fields so we can replace them with LSP snippet placeholders after formatting.
        root = AnnotateFields(root, textChange, snippetExpansion, out var fieldAnnotations);

        // Attach an annotation to the trivia indicating the final caret position (if it exists)
        root = AnnotateFinalCaretPosition(root, textChange, snippetExpansion, out var finalCaretAnnotation);

        // Format the document with inserted snippet and annotations.
        documentWithSnippet = originalDocument.WithSyntaxRoot(root);
        documentWithSnippet = await Formatter.FormatAsync(documentWithSnippet, new TextSpan(textChange.Span.Start, snippetFullText.Length), options: null, cancellationToken).ConfigureAwait(false);
        var formattedText = await documentWithSnippet.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Now that we have the snippet with default values formatted correctly, we need to replace the default values
        // with the appropriate syntax for LSP tab stops.
        root = await documentWithSnippet.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Extract the snippet span out of the formatted text.
        var spanContainingFormattedSnippet = TextSpan.FromBounds(
            root.GetAnnotatedTokens(startAnnotation).Single().Span.Start,
            root.GetAnnotatedTokens(endAnnotation).Single().Span.End);

        // Create text changes to replace the declarations with LSP formatted tab stops,
        // but in the context of the snippet, not the whole document.
        using var _1 = ArrayBuilder<TextChange>.GetInstance(out var lspTextChanges);
        foreach (var annotation in fieldAnnotations)
        {
            Contract.ThrowIfNull(annotation.Data);
            var nodesAndTokens = root.GetAnnotatedNodesAndTokens(annotation);
            foreach (var nodeOrToken in nodesAndTokens)
            {
                var textSpanInSnippet = GetTextSpanInContextOfSnippet(nodeOrToken.Span.Start, spanContainingFormattedSnippet.Start, nodeOrToken.Span.Length);
                var lspTextChange = new TextChange(textSpanInSnippet, annotation.Data);
                lspTextChanges.Add(lspTextChange);
            }
        }

        // Replace the final caret trivia annotation with the appropriate LSP placeholder text.
        if (finalCaretAnnotation != null)
        {
            Contract.ThrowIfNull(finalCaretAnnotation.Data);
            var trivia = root.GetAnnotatedTrivia(finalCaretAnnotation).Single();
            var textSpanInSnippet = GetTextSpanInContextOfSnippet(trivia.Span.Start, spanContainingFormattedSnippet.Start, trivia.Span.Length);
            lspTextChanges.Add(new TextChange(textSpanInSnippet, finalCaretAnnotation.Data));
        }

        // Apply all the text changes to get the text formatted as the LSP snippet syntax.
        var formattedLspSnippetText = formattedText.GetSubText(spanContainingFormattedSnippet).WithChanges(lspTextChanges);

        return formattedLspSnippetText.ToString();

        static SyntaxNode AnnotateSnippetBounds(SyntaxNode root, TextChange snippetTextChange, out SyntaxAnnotation startAnnotation, out SyntaxAnnotation endAnnotation)
        {
            startAnnotation = new SyntaxAnnotation("start");
            var startToken = root.FindTokenOnRightOfPosition(snippetTextChange.Span.Start);
            root = AnnotateToken(startToken, startAnnotation, root);

            endAnnotation = new SyntaxAnnotation("end");
            var endToken = root.FindTokenOnLeftOfPosition(snippetTextChange.Span.Start + snippetTextChange.NewText!.Length);
            root = AnnotateToken(endToken, endAnnotation, root);

            return root;
        }

        static SyntaxNode AnnotateFields(SyntaxNode root, TextChange snippetTextChange, ExpansionTemplate snippetExpansion, out ImmutableArray<SyntaxAnnotation> fieldAnnotations)
        {
            using var _ = ArrayBuilder<SyntaxAnnotation>.GetInstance(out var annotations);
            foreach (var field in snippetExpansion.Fields)
            {
                if (!field.IsEditable)
                {
                    // The field is not editable, we don't need to come back to this later and can just leave it with the default value.
                    continue;
                }

                // Generate the LSP formatted text required to insert this tabstop.
                var fieldTabStopIndex = field.GetTabStopIndex();
                var lspTextForField = string.IsNullOrEmpty(field.Default) ? $"${{{fieldTabStopIndex}}}" : $"${{{fieldTabStopIndex}:{field.Default}}}";

                // Store the LSP snippet text on the annotation as its easy to retrive later.
                var fieldAnnotation = new SyntaxAnnotation(field.ID, data: lspTextForField);

                // Find all the locations associated with this field and annotate the node.
                foreach (var offset in field.GetOffsets())
                {
                    var originalToken = root.FindTokenOnRightOfPosition(snippetTextChange.Span.Start + offset);
                    root = AnnotateToken(originalToken, fieldAnnotation, root);
                }

                annotations.Add(fieldAnnotation);
            }

            fieldAnnotations = annotations.ToImmutable();
            return root;
        }

        static SyntaxNode AnnotateFinalCaretPosition(SyntaxNode root, TextChange snippetTextChange, ExpansionTemplate snippetExpansion, out SyntaxAnnotation? finalCaretAnnotation)
        {
            var finalCaretTokenOffset = snippetExpansion.GetEndOffset();
            finalCaretAnnotation = null;
            if (finalCaretTokenOffset != null)
            {
                var lspTextForFinalCaret = "$0";
                finalCaretAnnotation = new SyntaxAnnotation("caret", lspTextForFinalCaret);

                // We always indicate the final caret position via trivia (/*$0*/)
                var originalTrivia = root.FindTrivia(snippetTextChange.Span.Start + finalCaretTokenOffset.Value);
                var annotatedTrivia = originalTrivia.WithAdditionalAnnotations(finalCaretAnnotation);
                root = root.ReplaceTrivia(originalTrivia, annotatedTrivia);
            }

            return root;
        }

        static TextSpan GetTextSpanInContextOfSnippet(int positionInFullText, int snippetPositionInFullText, int length)
        {
            var offsetInSnippet = new TextSpan(positionInFullText - snippetPositionInFullText, length);
            return offsetInSnippet;
        }

        static SyntaxNode AnnotateToken(SyntaxToken originalToken, SyntaxAnnotation annotation, SyntaxNode root)
        {
            var annotatedToken = originalToken.WithAdditionalAnnotations(annotation);
            var newRoot = root.ReplaceToken(originalToken, annotatedToken);
            return newRoot;
        }
    }

    private static bool TryGetWordOnLeft(int position, SourceText currentText, ISyntaxFactsService syntaxFactsService, [NotNullWhen(true)] out TextSpan? wordSpan)
    {
        var endPosition = position;
        var startPosition = endPosition;

        // Find the snippet shortcut
        while (startPosition > 0)
        {
            var c = currentText[startPosition - 1];
            if (!syntaxFactsService.IsIdentifierPartCharacter(c) && c != '#' && c != '~')
            {
                break;
            }

            startPosition--;
        }

        if (startPosition == endPosition)
        {
            wordSpan = null;
            return false;
        }

        wordSpan = TextSpan.FromBounds(startPosition, endPosition);
        return true;
    }

    private static CodeSnippet? RetrieveSnippetFromXml(string wordText, SnippetInfo snippetInfo, RequestContext context)
    {
        var path = snippetInfo.Path;
        if (path == null)
        {
            context.TraceInformation($"Missing file path for snippet {snippetInfo.Title}");
            return null;
        }

        if (!File.Exists(path))
        {
            context.TraceInformation($"Snippet {snippetInfo.Title} has an invalid file path: {snippetInfo.Path}");
            return null;
        }

        // Load the xml for the snippet from disk.
        // Any exceptions thrown here we allow to bubble up and let the queue log it.
        var snippets = CodeSnippet.ReadSnippetsFromFile(snippetInfo.Path);

        // There could be multiple snippets defined in the xml file containing the specified snippet info.
        // Match against the parsed snippets to find the correct one.
        var matchingSnippet = snippets.Single(s => wordText.Equals(s.Shortcut, StringComparison.OrdinalIgnoreCase));

        return matchingSnippet;
    }
}
