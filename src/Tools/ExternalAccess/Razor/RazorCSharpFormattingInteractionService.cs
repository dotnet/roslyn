// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// Enables Razor to utilize Roslyn's C# formatting service.
    /// </summary>
    internal static class RazorCSharpFormattingInteractionService
    {
        /// <summary>
        /// True if this service would like to format the document based on the user typing the
        /// provided character.
        /// </summary>
        public static bool SupportsFormattingOnTypedCharacter(Document document, char ch)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            var options = AutoFormattingOptions.From(document.Project);
            return formattingService.SupportsFormattingOnTypedCharacter(document, options, ch);
        }

        /// <summary>
        /// Returns the text changes necessary to format the document.  If "textSpan" is provided,
        /// only the text changes necessary to format that span are needed.
        /// </summary>
        public static Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            TextSpan? textSpan,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingService.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken);
        }

        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a 
        /// character.  The position provided is the position of the caret in the document after
        /// the character been inserted into the document.
        /// </summary>
        public static Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            char typedChar,
            int position,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingService.GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken);
        }

        /// <summary>
        /// Returns the text changes necessary to format the document on paste operation.
        /// </summary>
        public static Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(
            Document document,
            TextSpan textSpan,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingService.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken);
        }

        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a Return
        /// The position provided is the position of the caret in the document after Return.
        /// </summary>
        public static Task<ImmutableArray<TextChange>> GetFormattingChangesOnReturnAsync(
            Document document,
            int position,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            return formattingService.GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken);
        }
    }
}
