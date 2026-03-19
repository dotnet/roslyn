// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(FormatDocumentOnTypeHandler)), Shared]
[Method(Methods.TextDocumentOnTypeFormattingName)]
internal sealed class FormatDocumentOnTypeHandler : ILspServiceDocumentRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]?>
{
    private readonly IGlobalOptionService _globalOptions;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FormatDocumentOnTypeHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(DocumentOnTypeFormattingParams request) => request.TextDocument;

    public async Task<TextEdit[]?> HandleRequestAsync(
        DocumentOnTypeFormattingParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document is null)
            return null;

        if (string.IsNullOrEmpty(request.Character))
        {
            return [];
        }

        var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

        var formattingService = document.Project.Services.GetRequiredService<ISyntaxFormattingService>();
        var documentSyntax = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // The formatting service expects that the position is inside the token span associated with the typed character, but
        // in VSCode this is not always the case - the position the client gives us is not necessarily the position of the typed character.
        // For example when typing characters, the client may automatically
        //   1.  for '\n' - the client inserts indentation (to get '\n    ')
        //   2.  for '{' - the client inserts '}' (to get '{}')
        //
        // When the formatter calls root.FindToken, it may return a token different from what triggered the on type formatting, which causes us
        // to format way more than we want, depending on exactly where the position ends up.
        // Here we do our best to adjust the position back to the typed char location.
        if (text[position - 1] != request.Character[0])
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var adjustedToken = root.FindTokenOnLeftOfPosition(position);
            position = adjustedToken.Span.End;
        }

        // We should use the options passed in by LSP instead of the document's options.
        var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(request.Options, document, cancellationToken).ConfigureAwait(false);
        var indentationOptions = new IndentationOptions(formattingOptions)
        {
            AutoFormattingOptions = _globalOptions.GetAutoFormattingOptions(document.Project.Language)
        };

        if (!formattingService.ShouldFormatOnTypedCharacter(documentSyntax, request.Character[0], position, cancellationToken))
        {
            return [];
        }

        var textChanges = formattingService.GetFormattingChangesOnTypedCharacter(documentSyntax, position, indentationOptions, cancellationToken);
        if (textChanges.IsEmpty)
        {
            return [];
        }

        return [.. textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, documentSyntax.Text))];
    }
}
