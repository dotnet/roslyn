// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentHoverName, StringConstants.XamlLanguageName)]
    internal class HoverHandler : AbstractRequestHandler<TextDocumentPositionParams, Hover?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetTextDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var quickInfoService = document.Project.LanguageServices.GetService<IXamlQuickInfoService>();
            if (quickInfoService == null)
            {
                return null;
            }

            var info = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var descriptionBuilder = new List<TaggedText>(info.Description);
            var description = await info.Symbol.GetDescriptionAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (description.Any())
            {
                if (descriptionBuilder.Any())
                {
                    descriptionBuilder.AddLineBreak();
                }
                description.Concat(description);
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new VSHover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = GetMarkdownString(descriptionBuilder)
                },
                RawContent = new ClassifiedTextElement(descriptionBuilder.Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)))
            };

            // local functions
            // TODO - This should return correctly formatted markdown from tagged text.
            // https://github.com/dotnet/roslyn/issues/43387
            static string GetMarkdownString(IEnumerable<TaggedText> description)
                => string.Join("\r\n", description.Select(section => section.Text).Where(text => !string.IsNullOrEmpty(text)));
        }
    }
}
