// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(IFormattingInteractionService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptFormattingInteractionService : IFormattingInteractionService
    {
        private readonly IVSTypeScriptFormattingInteractionService _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal VSTypeScriptFormattingInteractionService(IVSTypeScriptFormattingInteractionService implementation)
            => _implementation = implementation;

        bool SupportsFormatDocument => _implementation.SupportsFormatDocument;
        bool SupportsFormatSelection => _implementation.SupportsFormatSelection;
        bool SupportsFormatOnPaste => _implementation.SupportsFormatOnPaste;
        bool SupportsFormatOnReturn => _implementation.SupportsFormatOnReturn;

        bool SupportsFormattingOnTypedCharacter(Document document, char ch)
            => _implementation.SupportsFormattingOnCharacterTyped(document, ch);

        Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _implementation.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken);

        Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _implementation.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions, cancellationToken);

        Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, char typedChar, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _implementation.GetFormattingChangesAsync(document, typedChar, position, documentOptions, cancellationToken);

        Task<ImmutableArray<TextChange>> GetFormattingChangesOnReturnAsync(Document document, int position, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _implementation.GetFormattingChangesOnReturnAsync(document, position, documentOptions, cancellationToken);
    }
}
