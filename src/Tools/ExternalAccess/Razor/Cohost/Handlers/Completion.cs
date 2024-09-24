// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class Completion
{
    private static CompletionListCache? s_completionListCache;

    private static CompletionListCache GetCache()
        => s_completionListCache ??= InterlockedOperations.Initialize(ref s_completionListCache, () => new());

    public static async Task<LSP.CompletionList?> GetCompletionListAsync(
        Document document,
        LinePosition linePosition,
        LSP.CompletionContext? completionContext,
        bool supportsVSExtensions,
        LSP.CompletionSetting completionCapabilities,
        CancellationToken cancellationToken)
    {
        var cache = GetCache();

        var position = await document
            .GetPositionFromLinePositionAsync(linePosition, cancellationToken)
            .ConfigureAwait(false);

        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var capabilityHelper = new CompletionCapabilityHelper(supportsVSExtensions, completionCapabilities);
        var completionOptions = globalOptions.GetCompletionOptionsForLsp(document.Project.Language, capabilityHelper);
        var completionListMaxSize = globalOptions.GetOption(LspOptionsStorage.MaxCompletionListSize);

        return await CompletionHandler.GetCompletionListAsync(
            document,
            position,
            completionOptions,
            completionContext,
            capabilityHelper,
            cache,
            completionListMaxSize,
            cancellationToken).ConfigureAwait(false);
    }

    public static Task<LSP.CompletionItem> ResolveCompletionItemAsync(
        LSP.CompletionItem completionItem,
        Document document,
        bool supportsVSExtensions,
        LSP.CompletionSetting completionCapabilities,
        CancellationToken cancellationToken)
    {
        var cache = GetCache();

        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var capabilityHelper = new CompletionCapabilityHelper(supportsVSExtensions, completionCapabilities);
        var completionOptions = globalOptions.GetCompletionOptions(document.Project.Language);
        var symbolDescriptionOptions = globalOptions.GetSymbolDescriptionOptions(document.Project.Language);

        return CompletionResolveHandler.ResolveCompletionItemAsync(
            completionItem, document, completionOptions, symbolDescriptionOptions, capabilityHelper, cache, cancellationToken);
    }
}
