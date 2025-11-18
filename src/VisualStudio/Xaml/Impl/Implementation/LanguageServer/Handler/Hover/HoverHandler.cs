// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.QuickInfo;
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;

[ExportStatelessXamlLspService(typeof(HoverHandler)), Shared]
[Method(Methods.TextDocumentHoverName)]
internal sealed class HoverHandler : ILspServiceRequestHandler<TextDocumentPositionParams, Hover?>
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public HoverHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

    public async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document == null)
        {
            return null;
        }

        var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

        var quickInfoService = document.Project.Services.GetService<IXamlQuickInfoService>();
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
            var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            var description = await info.Symbol.GetDescriptionAsync(document, options, cancellationToken).ConfigureAwait(false);
            if (description.Any())
            {
                if (descriptionBuilder.Any())
                {
                    descriptionBuilder.AddLineBreak();
                }

                descriptionBuilder.AddRange(description);
            }
        }

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return new VSInternalHover
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
