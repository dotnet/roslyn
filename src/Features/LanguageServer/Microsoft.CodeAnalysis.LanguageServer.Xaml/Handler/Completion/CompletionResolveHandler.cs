// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Xaml.Extensions;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle a completion resolve request to add description.
/// </summary>
[XamlMethod(Methods.TextDocumentCompletionResolveName)]
internal class CompletionResolveHandler : ILspServiceRequestHandler<CompletionItem, CompletionItem>, ITextDocumentIdentifierHandler<CompletionItem, TextDocumentIdentifier?>
{
    private readonly IGlobalOptionService _globalOptions;
    private readonly DocumentCache _documentCache;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public CompletionResolveHandler(IGlobalOptionService globalOptions, DocumentCache documentCache)
    {
        _globalOptions = globalOptions;
        _documentCache = documentCache;
    }

    public TextDocumentIdentifier? GetTextDocumentIdentifier(CompletionItem request)
        => GetTextDocumentCacheEntry(request);

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

        if (completionItem.Command?.CommandIdentifier is not null)
        {
            completionItem.Command.Arguments = completionItem.Command.Arguments?.Append(ProtocolConversions.DocumentToTextDocumentIdentifier(document));
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

    private TextDocumentIdentifier? GetTextDocumentCacheEntry(CompletionItem request)
    {
        Contract.ThrowIfNull(request.Data);
        var resolveData = ((JToken)request.Data).ToObject<DocumentIdResolveData>();
        if (resolveData?.DocumentId == null)
        {
            Contract.Fail("Document id should always be provided when resolving a completion item we returned.");
            return null;
        }

        var document = _documentCache.GetCachedEntry(resolveData.DocumentId);
        if (document == null)
        {
            // No cache for associated document id. Log some telemetry so we can understand how frequently this actually happens.
            Logger.Log(FunctionId.LSP_DocumentIdCacheMiss, KeyValueLogMessage.NoProperty);
        }

        return document;
    }
}
