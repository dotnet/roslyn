// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Acts as an internal proxy for getting an <see cref="IFormattingInteractionService"/> or, if unavailable, an <see cref="IEditorFormattingService"/>.
    /// This should be removed when <see cref="IEditorFormattingService"/> is fully removed.
    /// </summary>
    internal struct FormattingInteractionServiceProxy
    {
        public static FormattingInteractionServiceProxy? GetService(Document document)
        {
            var formattingService = document.GetLanguageService<IFormattingInteractionService>();
#pragma warning disable CS0618 // Type or member is obsolete
            IEditorFormattingService? editorService = null;

            if (formattingService == null && (editorService = document.GetLanguageService<IEditorFormattingService>()) == null)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                return null;
            }

            return new(formattingService, editorService);
        }

        public static FormattingInteractionServiceProxy GetRequiredService(Document document)
        {
            var formattingService = document.GetLanguageService<IFormattingInteractionService>();
#pragma warning disable CS0618 // Type or member is obsolete
            var editorService = formattingService == null ? document.GetRequiredLanguageService<IEditorFormattingService>() : null;
#pragma warning restore CS0618 // Type or member is obsolete

            return new(formattingService, editorService);
        }

        private readonly IFormattingInteractionService? _formattingService;
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly IEditorFormattingService? _editorService;
#pragma warning restore CS0618 // Type or member is obsolete

        private FormattingInteractionServiceProxy(
            IFormattingInteractionService? formattingService,
#pragma warning disable CS0618 // Type or member is obsolete
            IEditorFormattingService? editorService)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            Debug.Assert(formattingService != null || editorService != null);
            _formattingService = formattingService;
            _editorService = editorService;
        }

        public bool SupportsFormatDocument => _formattingService?.SupportsFormatDocument ?? _editorService!.SupportsFormatDocument;
        public bool SupportsFormatSelection => _formattingService?.SupportsFormatSelection ?? _editorService!.SupportsFormatSelection;
        public bool SupportsFormatOnPaste => _formattingService?.SupportsFormatOnPaste ?? _editorService!.SupportsFormatOnPaste;
        public bool SupportsFormatOnReturn => _formattingService?.SupportsFormatOnReturn ?? _editorService!.SupportsFormatOnReturn;

        /// <inheritdoc cref="IFormattingInteractionService.SupportsFormattingOnTypedCharacter(Document, char)"/>
        public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
            => _formattingService?.SupportsFormattingOnTypedCharacter(document, ch) ?? _editorService!.SupportsFormattingOnTypedCharacter(document, ch);

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesAsync(Document, TextSpan?, DocumentOptionSet?, CancellationToken)"/>
        public async Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            if (_formattingService != null)
            {
                return await _formattingService.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
            }

            return await _editorService!.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesOnPasteAsync(Document, TextSpan, DocumentOptionSet?, CancellationToken)"/>
        public async Task<IList<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            if (_formattingService != null)
            {
                return await _formattingService.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
            }

            return await _editorService!.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesAsync(Document, char, int, CodeAnalysis.Options.DocumentOptionSet?, CancellationToken)"/>
        public async Task<IList<TextChange>?> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            if (_formattingService != null)
            {
                return await _formattingService.GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken).ConfigureAwait(false);
            }

            return await _editorService!.GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IFormattingInteractionService.GetFormattingChangesOnReturnAsync(Document, int, DocumentOptionSet?, CancellationToken)"/>
        public async Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            if (_formattingService != null)
            {
                return await _formattingService.GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken).ConfigureAwait(false);
            }

            return await _editorService!.GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
