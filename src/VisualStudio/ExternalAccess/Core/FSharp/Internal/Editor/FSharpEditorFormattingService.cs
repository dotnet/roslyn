// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Editor;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor;
#endif

[Shared]
[ExportLanguageService(typeof(IFormattingInteractionService), LanguageNames.FSharp)]
internal class FSharpEditorFormattingService : IFormattingInteractionService
{
    private readonly IFSharpEditorFormattingService _service;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpEditorFormattingService(IFSharpEditorFormattingService service, IGlobalOptionService globalOptions)
    {
        _service = service;
        _globalOptions = globalOptions;
    }

    public bool SupportsFormatDocument => _service.SupportsFormatDocument;

    public bool SupportsFormatSelection => _service.SupportsFormatSelection;

    public bool SupportsFormatOnPaste => _service.SupportsFormatOnPaste;

    public bool SupportsFormatOnReturn => _service.SupportsFormatOnReturn;

    public Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, CancellationToken cancellationToken)
    {
        return _service.GetFormattingChangesAsync(document, textSpan, cancellationToken);
    }

    public Task<IList<TextChange>?> GetFormattingChangesAsync(Document document, char typedChar, int position, CancellationToken cancellationToken)
    {
        return _service.GetFormattingChangesAsync(document, typedChar, position, cancellationToken);
    }

    public Task<IList<TextChange>> GetFormattingChangesOnPasteAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        return _service.GetFormattingChangesOnPasteAsync(document, textSpan, cancellationToken);
    }

    public Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(Document document, int position, CancellationToken cancellationToken)
    {
        return _service.GetFormattingChangesOnReturnAsync(document, position, cancellationToken);
    }

    public bool SupportsFormattingOnTypedCharacter(Document document, char ch)
    {
        if (_service is IFSharpEditorFormattingServiceWithOptions serviceWithOptions)
        {
            var indentStyle = _globalOptions.GetOption(IndentationOptionsStorage.SmartIndent, LanguageNames.FSharp);
            var options = _globalOptions.GetAutoFormattingOptions(LanguageNames.FSharp);

            return serviceWithOptions.SupportsFormattingOnTypedCharacter(document, new AutoFormattingOptionsWrapper(options, indentStyle), ch);
        }
        else
        {
            return _service.SupportsFormattingOnTypedCharacter(document, ch);
        }
    }

    async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesAsync(Document document, ITextBuffer textBuffer, TextSpan? textSpan, CancellationToken cancellationToken)
    {
        var changes = await GetFormattingChangesAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        return changes?.ToImmutableArray() ?? [];
    }

    async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesAsync(Document document, ITextBuffer textBuffer, char typedChar, int position, CancellationToken cancellationToken)
    {
        var changes = await GetFormattingChangesAsync(document, typedChar, position, cancellationToken).ConfigureAwait(false);
        return changes?.ToImmutableArray() ?? [];
    }

    async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesOnPasteAsync(Document document, ITextBuffer textBuffer, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var changes = await GetFormattingChangesOnPasteAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        return changes?.ToImmutableArray() ?? [];
    }

    async Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesOnReturnAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var changes = await GetFormattingChangesOnReturnAsync(document, position, cancellationToken).ConfigureAwait(false);
        return changes?.ToImmutableArray() ?? [];
    }
}
