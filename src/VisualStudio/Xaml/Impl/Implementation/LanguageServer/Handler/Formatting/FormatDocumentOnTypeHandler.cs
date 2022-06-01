// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Formatting;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [ExportXamlLspRequestHandlerProvider(typeof(FormatDocumentOnTypeHandler)), Shared]
    [Method(Methods.TextDocumentOnTypeFormattingName)]
    internal class FormatDocumentOnTypeHandler : AbstractStatelessRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentOnTypeHandler()
        {
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentOnTypeFormattingParams request) => request.TextDocument;

        public override async Task<TextEdit[]> HandleRequestAsync(DocumentOnTypeFormattingParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var edits = new ArrayBuilder<TextEdit>();
            if (string.IsNullOrEmpty(request.Character))
            {
                return edits.ToArrayAndFree();
            }

            var document = context.Document;
            var formattingService = document?.Project.LanguageServices.GetService<IXamlFormattingService>();
            if (document != null && formattingService != null)
            {
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
                var options = new XamlFormattingOptions { InsertSpaces = request.Options.InsertSpaces, TabSize = request.Options.TabSize, OtherOptions = request.Options.OtherOptions };
                var textChanges = await formattingService.GetFormattingChangesAsync(document, options, request.Character[0], position, cancellationToken).ConfigureAwait(false);
                if (textChanges != null)
                {
                    var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
                }
            }

            return edits.ToArrayAndFree();
        }
    }
}
