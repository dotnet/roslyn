﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider(StringConstants.XamlLanguageName), Shared]
    [ProvidesMethod(Methods.TextDocumentHoverName)]
    internal class HoverHandler : AbstractStatelessRequestHandler<TextDocumentPositionParams, Hover?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler()
        {
        }

        public override string Method => Methods.TextDocumentHoverName;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public override async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
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
            if (info.Symbol != null)
            {
                var description = await info.Symbol.GetDescriptionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (description.Any())
                {
                    if (descriptionBuilder.Any())
                    {
                        descriptionBuilder.AddLineBreak();
                    }
                    descriptionBuilder.AddRange(description);
                }
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
