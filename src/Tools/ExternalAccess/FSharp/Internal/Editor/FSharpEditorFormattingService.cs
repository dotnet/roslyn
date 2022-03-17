// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(IFormattingInteractionService), LanguageNames.FSharp)]
    internal class FSharpEditorFormattingService : IFormattingInteractionService
    {
        private readonly IFSharpEditorFormattingService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpEditorFormattingService(IFSharpEditorFormattingService service)
        {
            _service = service;
        }

        public bool SupportsFormatDocument => _service.SupportsFormatDocument;

        public bool SupportsFormatSelection => _service.SupportsFormatSelection;

        public bool SupportsFormatOnPaste => _service.SupportsFormatOnPaste;

        public bool SupportsFormatOnReturn => _service.SupportsFormatOnReturn;

        public Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            return _service.GetFormattingChangesAsync(document, textSpan, cancellationToken);
        }

        public Task<IList<TextChange>?> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            return _service.GetFormattingChangesAsync(document, typedChar, position, cancellationToken);
        }

        public Task<IList<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            return _service.GetFormattingChangesOnPasteAsync(document, textSpan, cancellationToken);
        }

        public Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            return _service.GetFormattingChangesOnReturnAsync(document, position, cancellationToken);
        }

        public bool SupportsFormattingOnTypedCharacter(Document document, AutoFormattingOptions options, char ch)
        {
            return _service is IFSharpEditorFormattingServiceWithOptions serviceWithOptions ?
                serviceWithOptions.SupportsFormattingOnTypedCharacter(document, new AutoFormattingOptionsWrapper(options), ch) :
                _service.SupportsFormattingOnTypedCharacter(document, ch);
        }

        async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var changes = await GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
            return changes?.ToImmutableArray() ?? ImmutableArray<TextChange>.Empty;
        }

        async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var changes = await GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken).ConfigureAwait(false);
            return changes?.ToImmutableArray() ?? ImmutableArray<TextChange>.Empty;
        }

        async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var changes = await GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
            return changes?.ToImmutableArray() ?? ImmutableArray<TextChange>.Empty;
        }

        async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
        {
            var changes = await GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken).ConfigureAwait(false);
            return changes?.ToImmutableArray() ?? ImmutableArray<TextChange>.Empty;
        }
    }
}
