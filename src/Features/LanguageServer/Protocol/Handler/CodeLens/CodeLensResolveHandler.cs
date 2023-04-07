﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

[Method(LSP.Methods.CodeLensResolveName)]
internal sealed class CodeLensResolveHandler : ILspServiceDocumentRequestHandler<LSP.CodeLens, LSP.CodeLens>
{
    /// <summary>
    /// Command name implemented by the client and invoked when the references code lens is selected.
    /// </summary>
    private const string ClientReferencesCommand = "roslyn.client.peekReferences";

    private readonly CodeLensCache _codeLensCache;

    public CodeLensResolveHandler(CodeLensCache codeLensCache)
    {
        _codeLensCache = codeLensCache;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLens request)
        => GetCodeLensResolveData(request).TextDocument;

    public async Task<LSP.CodeLens> HandleRequestAsync(LSP.CodeLens request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var resolveData = GetCodeLensResolveData(request);
        var (cacheEntry, memberToResolve) = GetCacheEntry(resolveData);

        var currentSyntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
        var cachedSyntaxVersion = cacheEntry.SyntaxVersion;

        if (currentSyntaxVersion != cachedSyntaxVersion)
        {
            throw new LocalRpcException($"Cached resolve version {cachedSyntaxVersion} does not match current version {currentSyntaxVersion}")
            {
                ErrorCode = LspErrorCodes.ContentModified
            };
        }

        var codeLensReferencesService = document.Project.Solution.Services.GetRequiredService<ICodeLensReferencesService>();
        var referenceCount = await codeLensReferencesService.GetReferenceCountAsync(document.Project.Solution, document.Id, memberToResolve.Node, maxSearchResults: 99, cancellationToken).ConfigureAwait(false);
        if (referenceCount != null)
        {
            request.Command = new LSP.Command
            {
                Title = referenceCount.Value.GetDescription(),
                CommandIdentifier = ClientReferencesCommand,
                Arguments = new object[]
                {
                        resolveData.TextDocument.Uri,
                        request.Range.Start
                }
            };

        }

        return request;
    }

    private (CodeLensCache.CodeLensCacheEntry CacheEntry, CodeLensMember MemberToResolve) GetCacheEntry(CodeLensResolveData resolveData)
    {
        var cacheEntry = _codeLensCache.GetCachedEntry(resolveData.ResultId);
        Contract.ThrowIfNull(cacheEntry, "Missing cache entry for code lens resolve request");
        return (cacheEntry, cacheEntry.CodeLensMembers[resolveData.ListIndex]);
    }

    private static CodeLensResolveData GetCodeLensResolveData(LSP.CodeLens codeLens)
    {
        var resolveData = (codeLens.Data as JToken)?.ToObject<CodeLensResolveData>();
        Contract.ThrowIfNull(resolveData, "Missing data for code lens resolve request");
        return resolveData;
    }
}

