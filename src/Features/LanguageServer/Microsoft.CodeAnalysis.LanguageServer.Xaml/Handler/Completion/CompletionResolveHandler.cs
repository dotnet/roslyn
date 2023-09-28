// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Xaml.Extensions;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle a completion resolve request to add description.
/// </summary>
[ExportStatelessXamlLspService(typeof(CompletionResolveHandler)), Shared]
[XamlMethod(Methods.TextDocumentCompletionResolveName)]
internal class CompletionResolveHandler : ILspServiceRequestHandler<CompletionItem, CompletionItem>, ITextDocumentIdentifierHandler<CompletionItem, TextDocumentIdentifier?>
{
    private readonly IGlobalOptionService _globalOptions;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CompletionResolveHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public TextDocumentIdentifier? GetTextDocumentIdentifier(CompletionItem request)
        => ProtocolConversions.GetTextDocument(request.Data);

    public async Task<CompletionItem> HandleRequestAsync(CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        CompletionResolveData? data;
        if (completionItem.Data is JToken token)
        {
            data = token.ToObject<CompletionResolveData>();
            Assumes.Present(data);
        }
        else
        {
            return completionItem;
        }

        var document = context.TextDocument;
        if (document is null)
        {
            return completionItem;
        }

        var offset = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(data.Position), cancellationToken).ConfigureAwait(false);
        var completionService = document.Project.Services.GetService<IXamlCompletionService>();
        if (completionService is null)
        {
            return completionItem;
        }

        var symbol = await completionService.GetSymbolAsync(new XamlCompletionContext(document, offset), completionItem.Label, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (symbol is null)
        {
            return completionItem;
        }

        var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
        var description = await symbol.GetDescriptionAsync(document, options, cancellationToken).ConfigureAwait(false);
        if (description.Any())
        {
            var capabilityHelper = new CompletionCapabilityHelper(context.GetRequiredClientCapabilities());

            completionItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(description.ToImmutableArray(), document, capabilityHelper.SupportsMarkdownDocumentation);
        }

        return completionItem;
    }
}
