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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(FormatDocumentOnTypeHandler)), Shared]
    [Method(Methods.TextDocumentOnTypeFormattingName)]
    internal sealed class FormatDocumentOnTypeHandler : AbstractStatelessRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]?>
    {
        private readonly IGlobalOptionService _globalOptions;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentOnTypeHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentOnTypeFormattingParams request) => request.TextDocument;

        public override async Task<TextEdit[]?> HandleRequestAsync(
            DocumentOnTypeFormattingParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
                return null;

            var edits = new ArrayBuilder<TextEdit>();

            var formattingService = document.Project.LanguageServices.GetRequiredService<IFormattingInteractionService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(request.Character))
            {
                return edits.ToArrayAndFree();
            }

            IList<TextChange>? textChanges;
            if (SyntaxFacts.IsNewLine(request.Character[0]))
            {
                textChanges = await formattingService.GetFormattingChangesOnReturnAsync(
                    document, position, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // We should use the options passed in by LSP instead of the document's options.
                var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(request.Options, document, cancellationToken).ConfigureAwait(false);
                var indentationOptions = new IndentationOptions(formattingOptions, _globalOptions.GetAutoFormattingOptions(document.Project.Language));

                textChanges = await formattingService.GetFormattingChangesAsync(
                    document, request.Character[0], position, indentationOptions, cancellationToken).ConfigureAwait(false);
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (textChanges != null)
            {
                edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            }

            return edits.ToArrayAndFree();
        }
    }
}
