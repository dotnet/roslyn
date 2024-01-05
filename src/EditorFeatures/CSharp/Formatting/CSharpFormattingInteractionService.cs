﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportLanguageService(typeof(IFormattingInteractionService), LanguageNames.CSharp), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class CSharpFormattingInteractionService(EditorOptionsService editorOptionsService) : IFormattingInteractionService
    {
        // All the characters that might potentially trigger formatting when typed
        private static readonly char[] _supportedChars = ";{}#nte:)".ToCharArray();

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
            {
                return true;
            }

            var options = _editorOptionsService.GlobalOptions.GetAutoFormattingOptions(LanguageNames.CSharp);

            // If format-on-typing is not on, then we don't support formatting on any other characters.
            var autoFormattingOnTyping = options.FormatOnTyping;
            if (!autoFormattingOnTyping)
            {
                return false;
            }

            if (ch == '}' && !options.FormatOnCloseBrace)
            {
                return false;
            }

            if (ch == ';' && !options.FormatOnSemicolon)
            {
                return false;
            }

            // don't auto format after these keys if smart indenting is not on.
            if (ch is '#' or 'n' && !isSmartIndent)
            {
                return false;
            }

            return _supportedChars.Contains(ch);
        }

        public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            ITextBuffer textBuffer,
            TextSpan? textSpan,
            CancellationToken cancellationToken)
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var options = textBuffer.GetSyntaxFormattingOptions(_editorOptionsService, parsedDocument.LanguageServices, explicitFormat: true);

            var span = textSpan ?? new TextSpan(0, parsedDocument.Root.FullSpan.Length);
            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(parsedDocument.Root, span);

            return Task.FromResult(Formatter.GetFormattedTextChanges(parsedDocument.Root, SpecializedCollections.SingletonEnumerable(formattingSpan), document.Project.Solution.Services, options, cancellationToken).ToImmutableArray());
        }

        public Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, ITextBuffer textBuffer, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var options = textBuffer.GetSyntaxFormattingOptions(_editorOptionsService, parsedDocument.LanguageServices, explicitFormat: true);
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
                var indentationOptions = textBuffer.GetIndentationOptions(_editorOptionsService, parsedDocument.LanguageServices, explicitFormat: false);
                return Task.FromResult(service.GetFormattingChangesOnTypedCharacter(parsedDocument, position, indentationOptions, cancellationToken));
            }

            return SpecializedTasks.EmptyImmutableArray<TextChange>();
        }
    }
}
