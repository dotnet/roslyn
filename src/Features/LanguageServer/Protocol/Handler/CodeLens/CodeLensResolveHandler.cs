// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;
using Microsoft.CodeAnalysis.Shared.Extensions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Collections.Immutable;

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
        var currentSyntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

        var cacheEntry = _codeLensCache.GetCachedEntry(currentSyntaxVersion.ToString());
        ImmutableArray<CodeLensMember> members;

        if (cacheEntry == null)
        {
            var codeLensMemberFinder = document.GetRequiredLanguageService<ICodeLensMemberFinder>();
            members = await codeLensMemberFinder.GetCodeLensMembersAsync(document, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            members = cacheEntry.CodeLensMembers;
        }

        var resolveData = GetCodeLensResolveData(request);
        var memberToResolve = members[resolveData.ListIndex];

        request.Command = new LSP.Command
        {
            Title = string.Format(FeaturesResources._0_references_unquoted, "-"),
            CommandIdentifier = ClientReferencesCommand,
            Arguments = new object[]
            {
                   resolveData.TextDocument.Uri,
                   request.Range.Start
            }
        };

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

    private static CodeLensResolveData GetCodeLensResolveData(LSP.CodeLens codeLens)
    {
        var resolveData = (codeLens.Data as JToken)?.ToObject<CodeLensResolveData>();
        Contract.ThrowIfNull(resolveData, "Missing data for code lens resolve request");
        return resolveData;
    }
}

