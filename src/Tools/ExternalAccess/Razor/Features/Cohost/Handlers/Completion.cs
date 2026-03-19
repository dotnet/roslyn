// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class Completion
{
    public static async Task<LSP.VSInternalCompletionList?> GetCompletionListAsync(
        Document document,
        LinePosition linePosition,
        LSP.CompletionContext? completionContext,
        bool supportsVSExtensions,
        LSP.CompletionSetting completionCapabilities,
        CompletionListCacheWrapper cacheWrapper,
        CancellationToken cancellationToken)
    {
        var cache = cacheWrapper.GetCache();
        var position = await document
            .GetPositionFromLinePositionAsync(linePosition, cancellationToken)
            .ConfigureAwait(false);

        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var capabilityHelper = new CompletionCapabilityHelper(supportsVSExtensions, completionCapabilities);

        return await CompletionHandler.GetCompletionListAsync(
            document,
            position,
            completionContext,
            globalOptions,
            capabilityHelper,
            cache,
            cancellationToken).ConfigureAwait(false);
    }

    public static Task<LSP.CompletionItem> ResolveCompletionItemAsync(
        LSP.CompletionItem completionItem,
        Document document,
        bool supportsVSExtensions,
        LSP.CompletionSetting completionCapabilities,
        CompletionListCacheWrapper cacheWrapper,
        CancellationToken cancellationToken)
    {
        var cache = cacheWrapper.GetCache();

        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var capabilityHelper = new CompletionCapabilityHelper(supportsVSExtensions, completionCapabilities);

        return CompletionResolveHandler.ResolveCompletionItemAsync(
            completionItem, document, globalOptions, capabilityHelper, cache, cancellationToken);
    }

    public static Task<LSP.VSInternalInlineCompletionItem?> GetInlineCompletionItemsAsync(
        RazorCohostRequestContext? context,
        Document document,
        LinePosition position,
        LSP.FormattingOptions options,
        CancellationToken cancellationToken)
    {
        // Razor can't construct a RazorCohostRequestContext so we need to handle the null case, for their tests
        var logger = context is { } razorContext ? razorContext.GetRequiredService<ILspLogger>() : NoOpLspLogger.Instance;
        var xmlSnippetParser = document.Project.Solution.Services.ExportProvider.GetService<XmlSnippetParser>();

        return InlineCompletionsHandler.GetInlineCompletionItemsAsync(logger, document, position, options, xmlSnippetParser, cancellationToken);
    }
}
