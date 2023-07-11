// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
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

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(request.Character) || SyntaxFacts.IsNewLine(request.Character[0]))
            {
                return Array.Empty<TextEdit>();
            }

            var formattingService = document.Project.Services.GetRequiredService<ISyntaxFormattingService>();
            var documentSyntax = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (!formattingService.ShouldFormatOnTypedCharacter(documentSyntax, request.Character[0], position, cancellationToken))
            {
                return Array.Empty<TextEdit>();
            }

            // We should use the options passed in by LSP instead of the document's options.
            var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(request.Options, document, _globalOptions, cancellationToken).ConfigureAwait(false);
            var indentationOptions = new IndentationOptions(formattingOptions)
            {
                AutoFormattingOptions = _globalOptions.GetAutoFormattingOptions(document.Project.Language)
            };

            var textChanges = formattingService.GetFormattingChangesOnTypedCharacter(documentSyntax, position, indentationOptions, cancellationToken);
            if (textChanges.IsEmpty)
            {
                return Array.Empty<TextEdit>();
            }

            var edits = new ArrayBuilder<TextEdit>();
            edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, documentSyntax.Text)));
            return edits.ToArrayAndFree();
        }
    }
}
