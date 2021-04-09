// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Formatting
{
    [ExportLanguageService(typeof(IEditorFormattingService), LanguageNames.CSharp), Shared]
    internal class CSharpEditorFormattingService : IEditorFormattingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorFormattingService()
        {
        }

        public bool SupportsFormatDocument => CSharpFormattingInteractionService.SupportsFormatDocumentConstant;
        public bool SupportsFormatSelection => CSharpFormattingInteractionService.SupportsFormatSelectionConstant;
        public bool SupportsFormatOnPaste => CSharpFormattingInteractionService.SupportsFormatOnPasteConstant;
        public bool SupportsFormatOnReturn => CSharpFormattingInteractionService.SupportsFormatOnReturnConstant;

        public Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var formattingInteractionService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingInteractionService.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken);
        }

        public Task<IList<TextChange>?> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var formattingInteractionService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingInteractionService.GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken);
        }

        public Task<IList<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var formattingInteractionService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingInteractionService.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken);
        }

        public Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var formattingInteractionService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingInteractionService.GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken);
        }

        public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
        {
            var formattingInteractionService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingInteractionService.SupportsFormattingOnTypedCharacter(document, ch);
        }
    }
}
