// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Deprecated. Please use <see cref="IFormattingInteractionService"/> if available. <see cref="FormattingInteractionServiceProxy"/> is available
    /// for wrapping the logic of checking for a formatting interaction service and falling back to this interface.
    /// </summary>
    [Obsolete("Move to IFormattingService now")]
    internal interface IEditorFormattingService : ILanguageService
    {
        bool SupportsFormatDocument { get; }
        bool SupportsFormatSelection { get; }
        bool SupportsFormatOnPaste { get; }
        bool SupportsFormatOnReturn { get; }

        /// <inheritdoc cref="IFormattingInteractionService.SupportsFormattingOnTypedCharacter(Document, char)"/>
        bool SupportsFormattingOnTypedCharacter(Document document, char ch);

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesAsync(Document, TextSpan?, DocumentOptionSet?, CancellationToken)"/>
        Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesOnPasteAsync(Document, TextSpan, DocumentOptionSet?, CancellationToken)"/>
        Task<IList<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesAsync(Document, char, int, DocumentOptionSet?, CancellationToken)"/>
        Task<IList<TextChange>?> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesOnReturnAsync(Document, int, DocumentOptionSet?, CancellationToken)"/>
        Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken);
    }
}
