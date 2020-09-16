// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentHoverName)]
    internal class HoverHandler : AbstractRequestHandler<TextDocumentPositionParams, Hover?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document == null)
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var quickInfoService = document.Project.LanguageServices.GetRequiredService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // TODO - Switch to markup content once it supports classifications.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
            return new VSHover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = new SumType<SumType<string, MarkedString>, SumType<string, MarkedString>[], MarkupContent>(string.Empty),
                // Build the classified text without navigation actions - they are not serializable.
                RawContent = await IntellisenseQuickInfoBuilder.BuildContentWithoutNavigationActionsAsync(info, document, cancellationToken).ConfigureAwait(false)
            };
        }
    }
}
