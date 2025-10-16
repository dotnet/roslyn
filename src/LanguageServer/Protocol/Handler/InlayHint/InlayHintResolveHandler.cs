// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;

[ExportCSharpVisualBasicStatelessLspService(typeof(InlayHintResolveHandler)), Shared]
[Method(Methods.InlayHintResolveName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class InlayHintResolveHandler(IGlobalOptionService globalOptions) : ILspServiceDocumentRequestHandler<LSP.InlayHint, LSP.InlayHint>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.InlayHint request)
        => GetInlayHintResolveData(request).TextDocument;

    public Task<LSP.InlayHint> HandleRequestAsync(LSP.InlayHint request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var options = globalOptions.GetInlineHintsOptions(document.Project.Language);
        var inlayHintCache = context.GetRequiredService<InlayHintCache>();
        var resolveData = GetInlayHintResolveData(request);
        return ResolveInlayHintAsync(document, request, inlayHintCache, resolveData, options, cancellationToken);
    }

    internal static async Task<LSP.InlayHint> ResolveInlayHintAsync(
        Document document,
        LSP.InlayHint request,
        InlayHintCache inlayHintCache,
        InlayHintResolveData resolveData,
        InlineHintsOptions options,
        CancellationToken cancellationToken)
    {
        var currentSyntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
        var resolveSyntaxVersion = resolveData.SyntaxVersion;

        if (currentSyntaxVersion.ToString() != resolveSyntaxVersion)
        {
            throw new LocalRpcException($"Request resolve version {resolveSyntaxVersion} does not match current version {currentSyntaxVersion}")
            {
                ErrorCode = LspErrorCodes.ContentModified
            };
        }

        var inlineHintToResolve = GetCacheEntry(resolveData, inlayHintCache);
        if (inlineHintToResolve is null)
        {
            // It is very possible that the cache no longer contains the hint being resolved (for example, multiple documents open side by side).
            // Instead of throwing, we can recompute the hints since we've already verified above that the version has not changed.
            var hints = await InlayHintHandler.CalculateInlayHintsAsync(document, resolveData.Range, options, resolveData.DisplayAllOverride, cancellationToken).ConfigureAwait(false);
            inlineHintToResolve = hints[resolveData.ListIndex];
        }

        var taggedText = await inlineHintToResolve.Value.GetDescriptionAsync(document, cancellationToken).ConfigureAwait(false);

        request.ToolTip = ProtocolConversions.GetDocumentationMarkupContent(taggedText, document, true);
        return request;
    }

    private static InlineHint? GetCacheEntry(InlayHintResolveData resolveData, InlayHintCache inlayHintCache)
    {
        var cacheEntry = inlayHintCache.GetCachedEntry(resolveData.ResultId);
        return cacheEntry?.InlayHintMembers[resolveData.ListIndex];
    }

    internal static InlayHintResolveData GetInlayHintResolveData(LSP.InlayHint inlayHint)
    {
        Contract.ThrowIfNull(inlayHint.Data);
        var resolveData = JsonSerializer.Deserialize<InlayHintResolveData>((JsonElement)inlayHint.Data, ProtocolConversions.LspJsonSerializerOptions);
        Contract.ThrowIfNull(resolveData, "Missing data for inlay hint resolve request");
        return resolveData;
    }
}
