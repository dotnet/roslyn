﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptFormattingInteractionService
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
        /// Returns the text changes necessary to format the document.  If "textSpan" is provided,
        /// only the text changes necessary to format that span are needed.
        /// </summary>
        Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text changes necessary to format the document on paste operation.
        /// </summary>
        Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a 
        /// character.  The position provided is the position of the caret in the document after
        /// the character been inserted into the document.
        /// </summary>
        Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a Return
        /// The position provided is the position of the caret in the document after Return.</summary>
        Task<ImmutableArray<TextChange>> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);
    }
}
