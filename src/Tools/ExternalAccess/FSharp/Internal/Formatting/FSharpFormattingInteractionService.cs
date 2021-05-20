// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Formatting
{
    [Shared]
    [ExportLanguageService(typeof(IFormattingInteractionService), LanguageNames.FSharp)]
    internal class FSharpFormattingInteractionService : IFormattingInteractionService
    {
        private readonly IFSharpFormattingInteractionService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpFormattingInteractionService(IFSharpFormattingInteractionService service)
        {
            _service = service;
        }

        public bool SupportsFormatDocument => _service.SupportsFormatDocument;

        public bool SupportsFormatSelection => _service.SupportsFormatSelection;

        public bool SupportsFormatOnPaste => _service.SupportsFormatOnPaste;

        public bool SupportsFormatOnReturn => _service.SupportsFormatOnReturn;

        public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _service.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken);

        public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _service.GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken);

        public Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _service.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken);

        public Task<ImmutableArray<TextChange>> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _service.GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken);

        public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
            => _service.SupportsFormattingOnTypedCharacter(document, ch);
    }
}
