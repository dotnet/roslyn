// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLanguageService(typeof(IFormattingInteractionService), InternalLanguageNames.TypeScript), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptFormattingInteractionService(IVSTypeScriptFormattingInteractionService implementation) : IFormattingInteractionService
{
    private readonly IVSTypeScriptFormattingInteractionService _implementation = implementation;

    public bool SupportsFormatDocument => _implementation.SupportsFormatDocument;
    public bool SupportsFormatSelection => _implementation.SupportsFormatSelection;
    public bool SupportsFormatOnPaste => _implementation.SupportsFormatOnPaste;
    public bool SupportsFormatOnReturn => _implementation.SupportsFormatOnReturn;

    public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
        => _implementation.SupportsFormattingOnTypedCharacter(document, ch);

    public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, ITextBuffer textBuffer, TextSpan? textSpan, CancellationToken cancellationToken)
        => _implementation.GetFormattingChangesAsync(document, textSpan, documentOptions: null, cancellationToken);

    public Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(Document document, ITextBuffer textBuffer, TextSpan textSpan, CancellationToken cancellationToken)
        => _implementation.GetFormattingChangesOnPasteAsync(document, textSpan, documentOptions: null, cancellationToken);

    public Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(Document document, ITextBuffer textBuffer, char typedChar, int position, CancellationToken cancellationToken)
        => _implementation.GetFormattingChangesAsync(document, typedChar, position, documentOptions: null, cancellationToken);

    public Task<ImmutableArray<TextChange>> GetFormattingChangesOnReturnAsync(Document document, int position, CancellationToken cancellationToken)
        => _implementation.GetFormattingChangesOnReturnAsync(document, position, documentOptions: null, cancellationToken);
}
