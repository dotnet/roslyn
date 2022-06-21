// Licensed to the .NET Foundation under one or more agreements.
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
    internal partial class CSharpFormattingInteractionService : IFormattingInteractionService
    {
        // All the characters that might potentially trigger formatting when typed
        private static readonly char[] _supportedChars = ";{}#nte:)".ToCharArray();

        private readonly IIndentationManagerService _indentationManager;
        private readonly IEditorOptionsFactoryService _editorOptionsFactory;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFormattingInteractionService(IIndentationManagerService indentationManager, IEditorOptionsFactoryService editorOptionsFactory, IGlobalOptionService globalOptions)
        {
            _indentationManager = indentationManager;
            _editorOptionsFactory = editorOptionsFactory;
            _globalOptions = globalOptions;
        }

        public bool SupportsFormatDocument => true;
        public bool SupportsFormatOnPaste => true;
        public bool SupportsFormatSelection => true;
        public bool SupportsFormatOnReturn => false;

        public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
        {
            var isSmartIndent = _globalOptions.GetOption(IndentationOptionsStorage.SmartIndent, LanguageNames.CSharp) == FormattingOptions2.IndentStyle.Smart;

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

            var options = _globalOptions.GetAutoFormattingOptions(LanguageNames.CSharp);

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

        public async Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            ITextBuffer textBuffer,
            TextSpan? textSpan,
            CancellationToken cancellationToken)
        {
            var fallbackOptions = _globalOptions.GetCSharpSyntaxFormattingOptions();
            var options = _indentationManager.GetInferredFormattingOptions(textBuffer, _editorOptionsFactory, document.Project.LanguageServices, fallbackOptions, explicitFormat: true);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var span = textSpan ?? new TextSpan(0, root.FullSpan.Length);
            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, span);

            var services = document.Project.Solution.Workspace.Services;
            return Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(formattingSpan), services, options, cancellationToken).ToImmutableArray();
        }

        public async Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, ITextBuffer textBuffer, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var fallbackOptions = _globalOptions.GetCSharpSyntaxFormattingOptions();
            var options = _indentationManager.GetInferredFormattingOptions(textBuffer, _editorOptionsFactory, document.Project.LanguageServices, fallbackOptions, explicitFormat: true);
            var service = document.GetRequiredLanguageService<ISyntaxFormattingService>();
            var documentSyntax = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            return service.GetFormattingChangesOnPaste(documentSyntax, textSpan, options, cancellationToken);
        }

        Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesOnReturnAsync(
            Document document, int caretPosition, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<TextChange>();

        public async Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, ITextBuffer textBuffer, char typedChar, int position, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<ISyntaxFormattingService>();
            var documentSyntax = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (service.ShouldFormatOnTypedCharacter(documentSyntax, typedChar, position, cancellationToken))
            {
                var fallbackOptions = _globalOptions.GetCSharpSyntaxFormattingOptions();
                var formattingOptions = _indentationManager.GetInferredFormattingOptions(textBuffer, _editorOptionsFactory, document.Project.LanguageServices, fallbackOptions, explicitFormat: false);

                var indentationOptions = new IndentationOptions(formattingOptions)
                {
                    AutoFormattingOptions = _globalOptions.GetAutoFormattingOptions(LanguageNames.CSharp),
                    IndentStyle = _globalOptions.GetOption(IndentationOptionsStorage.SmartIndent, LanguageNames.CSharp)
                };

                return service.GetFormattingChangesOnTypedCharacter(documentSyntax, position, indentationOptions, cancellationToken);
            }

            return ImmutableArray<TextChange>.Empty;
        }
    }
}
