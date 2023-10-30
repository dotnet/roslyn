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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.QuickInfo;
using Microsoft.CodeAnalysis.LanguageServer.Xaml.Extensions;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(HoverHandler)), Shared]
[XamlMethod(Methods.TextDocumentHoverName)]
internal sealed class HoverHandler : ILspServiceDocumentRequestHandler<TextDocumentPositionParams, Hover?>
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
        var document = context.TextDocument;
        if (document is null)
        {
            return null;
        }

        var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

        var quickInfoService = document.Project.Services.GetService<IXamlQuickInfoService>();
        if (quickInfoService is null)
        {
            return null;
        }

        var xamlInfo = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (xamlInfo is null)
        {
            return null;
        }

        var descriptionBuilder = new List<TaggedText>(xamlInfo.Description);

        if (xamlInfo.Symbol != null)
        {
            var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            var description = await xamlInfo.Symbol.GetDescriptionAsync(document, options, cancellationToken).ConfigureAwait(false);
            if (description.Any())
            {
                if (descriptionBuilder.Any())
                {
                    descriptionBuilder.AddLineBreak();
                }

                descriptionBuilder.AddRange(description);
            }
        }

        var clientCapabilities = context.GetRequiredClientCapabilities();
        var quickInfo = QuickInfoItem.Create(xamlInfo.Span, sections: new[]
        {
            QuickInfoSection.Create(QuickInfoSectionKinds.Description, descriptionBuilder.ToImmutableArray())
        }.ToImmutableArray());

        return await DefaultLspHoverResultCreationService.CreateDefaultHoverAsync(document, quickInfo, clientCapabilities, cancellationToken).ConfigureAwait(false);
    }
}
