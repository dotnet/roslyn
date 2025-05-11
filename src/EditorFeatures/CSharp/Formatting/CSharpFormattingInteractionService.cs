// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[ExportLanguageService(typeof(IFormattingInteractionService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpFormattingInteractionService(EditorOptionsService editorOptionsService) : IFormattingInteractionService
{
    /// <summary>
    /// All the characters that might potentially trigger formatting when typed.  The punctuation characters are the
    /// normal C# punctuation characters that start/end constructs we want to format when starting/ending them.  The
    /// letters 'n', 't', and 'e' are the ending characters of certain identifiers that we want to format when they are
    /// completely written.  For example, if the user types <c>#</c> we left align the preprocessor directive
    /// immediately.  However, once they type <c>region</c> (which ends with 'n') we then want to indent it.  See <see
    /// cref="CSharpSyntaxFormattingService.ValidSingleOrMultiCharactersTokenKind"/> for those identifier cases.
    /// </summary>
    private const string s_supportedChars = ";{}#:)nte";

    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;

    public bool SupportsFormatDocument => true;
    public bool SupportsFormatOnPaste => true;
    public bool SupportsFormatSelection => true;
    public bool SupportsFormatOnReturn => false;

    public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
    {
        var isSmartIndent = _editorOptionsService.GlobalOptions.GetOption(IndentationOptionsStorage.SmartIndent, LanguageNames.CSharp) == FormattingOptions2.IndentStyle.Smart;

        // We consider the proper placement of a close curly or open curly when it is typed at
        // the start of the line to be a smart-indentation operation.  As such, even if "format
        // on typing" is off, if "smart indent" is on, we'll still format this.  (However, we
        // won't touch anything else in the block this close curly belongs to.).
        //
        // See extended comment in GetFormattingChangesAsync for more details on this.
        if (isSmartIndent && ch is '{' or '}')
            return true;

        var options = _editorOptionsService.GlobalOptions.GetAutoFormattingOptions(LanguageNames.CSharp);

        // If format-on-typing is not on, then we don't support formatting on any other characters.
        var autoFormattingOnTyping = options.FormatOnTyping;
        if (!autoFormattingOnTyping)
            return false;

        if (ch == '}' && !options.FormatOnCloseBrace)
            return false;

        if (ch == ';' && !options.FormatOnSemicolon)
            return false;

        // don't auto format after these keys if smart indenting is not on.
        if (ch is '#' or 'n' && !isSmartIndent)
            return false;

        return s_supportedChars.IndexOf(ch) >= 0;
    }

    public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
        Document document,
        ITextBuffer textBuffer,
        TextSpan? textSpan,
        CancellationToken cancellationToken)
    {
        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var options = textBuffer.GetSyntaxFormattingOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), parsedDocument.LanguageServices, explicitFormat: true);

        var span = textSpan ?? new TextSpan(0, parsedDocument.Root.FullSpan.Length);
        var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(parsedDocument.Root, span);

        return Task.FromResult(Formatter.GetFormattedTextChanges(parsedDocument.Root, [formattingSpan], document.Project.Solution.Services, options, cancellationToken).ToImmutableArray());
    }

    public Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, ITextBuffer textBuffer, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var options = textBuffer.GetSyntaxFormattingOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), parsedDocument.LanguageServices, explicitFormat: true);
        var service = parsedDocument.LanguageServices.GetRequiredService<ISyntaxFormattingService>();
        return Task.FromResult(service.GetFormattingChangesOnPaste(parsedDocument, textSpan, options, cancellationToken));
    }

    public Task<ImmutableArray<TextChange>> GetFormattingChangesOnReturnAsync(Document document, int caretPosition, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyImmutableArray<TextChange>();

    public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, ITextBuffer textBuffer, char typedChar, int position, CancellationToken cancellationToken)
    {
        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var service = parsedDocument.LanguageServices.GetRequiredService<ISyntaxFormattingService>();

        if (service.ShouldFormatOnTypedCharacter(parsedDocument, typedChar, position, cancellationToken))
        {
            var indentationOptions = textBuffer.GetIndentationOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), parsedDocument.LanguageServices, explicitFormat: false);
            return Task.FromResult(service.GetFormattingChangesOnTypedCharacter(parsedDocument, position, indentationOptions, cancellationToken));
        }

        return SpecializedTasks.EmptyImmutableArray<TextChange>();
    }
}
