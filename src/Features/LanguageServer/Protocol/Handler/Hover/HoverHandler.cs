// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentHoverName)]
    internal class HoverHandler : IRequestHandler<TextDocumentPositionParams, Hover>
    {
        public async Task<Hover> HandleRequestAsync(Solution solution, TextDocumentPositionParams request,
            ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var quickInfoService = document.Project.LanguageServices.GetService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new Hover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = GetMarkdownString(info)
                }
            };

            // local functions
            // TODO - This should return correctly formatted markdown from quick info.
            // https://github.com/dotnet/roslyn/projects/45#card-20033878
            static string GetMarkdownString(QuickInfoItem info)
            {
                var stringBuilder = new StringBuilder();
                var description = info.Sections.FirstOrDefault(s => QuickInfoSectionKinds.Description.Equals(s.Kind))?.Text ?? string.Empty;
                var documentation = info.Sections.FirstOrDefault(s => QuickInfoSectionKinds.DocumentationComments.Equals(s.Kind))?.Text ?? string.Empty;

                if (!string.IsNullOrEmpty(description))
                {
                    stringBuilder.Append(description);
                    if (!string.IsNullOrEmpty(documentation))
                    {
                        stringBuilder.Append("\r\n> ").Append(documentation);
                    }
                }

                return stringBuilder.ToString();
            }
        }
    }
}
