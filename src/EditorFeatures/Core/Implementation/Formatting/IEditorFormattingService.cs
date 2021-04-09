// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IEditorFormattingService : ILanguageService
    {
        bool SupportsFormatDocument { get; }
        bool SupportsFormatSelection { get; }
        bool SupportsFormatOnPaste { get; }
        bool SupportsFormatOnReturn { get; }

        /// <summary>
        /// True if this service would like to format the document based on the user typing the
        /// provided character.
        /// </summary>
        bool SupportsFormattingOnTypedCharacter(Document document, char ch);

        /// <summary>
        /// Returns the text changes necessary to format the document using optional custom document options.
        /// If "textSpan" is provided, only the text changes necessary to format that span are needed.
        /// </summary>
        Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text changes necessary to format the document on paste operation using
        /// optional custom document options.
        /// </summary>
        Task<IList<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a 
        /// character using optional custom document options.  The position provided is the
        /// position of the caret in the document after the character been inserted into the
        /// document.
        /// </summary>
        Task<IList<TextChange>?> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a Return
        /// using optional custom document options. The position provided is the position of the caret
        /// in the document after Return.
        /// </summary>
        Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);
    }
}
