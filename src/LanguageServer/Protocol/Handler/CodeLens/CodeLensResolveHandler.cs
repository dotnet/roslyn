// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using LSP = Roslyn.LanguageServer.Protocol;
using System.Text.Json;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

[Method(LSP.Methods.CodeLensResolveName)]
internal sealed class CodeLensResolveHandler : ILspServiceDocumentRequestHandler<LSP.CodeLens, LSP.CodeLens>
{
    /// <summary>
    /// Command name implemented by the client and invoked when the references code lens is selected.
    /// </summary>
    private const string ClientReferencesCommand = "roslyn.client.peekReferences";

    public CodeLensResolveHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLens request)
        => GetCodeLensResolveData(request).TextDocument;

    public async Task<LSP.CodeLens> HandleRequestAsync(LSP.CodeLens request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var currentDocumentSyntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
        var resolveData = GetCodeLensResolveData(request);

        request.Command = new LSP.Command
        {
            Title = string.Format(FeaturesResources._0_references_unquoted, "-"),
            CommandIdentifier = ClientReferencesCommand,
            Arguments =
            [
                resolveData.TextDocument.Uri,
                request.Range.Start
            ]
        };

        // If the request is for an older version of the document, throw an exception so the client knows to re-query us.
        if (resolveData.SyntaxVersion != currentDocumentSyntaxVersion.ToString())
        {
            throw new LocalRpcException($"Resolve version {resolveData.SyntaxVersion} does not match current version {currentDocumentSyntaxVersion}")
            {
                ErrorCode = LspErrorCodes.ContentModified
            };
        }

        var codeLensMemberFinder = document.GetRequiredLanguageService<ICodeLensMemberFinder>();
        var members = await codeLensMemberFinder.GetCodeLensMembersAsync(document, cancellationToken).ConfigureAwait(false);

        var memberToResolve = members[resolveData.ListIndex];
        var codeLensReferencesService = document.Project.Solution.Services.GetRequiredService<ICodeLensReferencesService>();
        var referenceCount = await codeLensReferencesService.GetReferenceCountAsync(document.Project.Solution, document.Id, memberToResolve.Node, maxSearchResults: 99, cancellationToken).ConfigureAwait(false);
        if (referenceCount != null)
        {
            request.Command = new LSP.Command
            {
                Title = referenceCount.Value.GetDescription(),
                CommandIdentifier = ClientReferencesCommand,
                Arguments =
                [
                    resolveData.TextDocument.Uri,
                    request.Range.Start,
                ],
            };

        }

        return request;
    }

    private static CodeLensResolveData GetCodeLensResolveData(LSP.CodeLens codeLens)
    {
        Contract.ThrowIfNull(codeLens.Data);
        var resolveData = JsonSerializer.Deserialize<CodeLensResolveData>((JsonElement)codeLens.Data, ProtocolConversions.LspJsonSerializerOptions);
        Contract.ThrowIfNull(resolveData, "Missing data for code lens resolve request");
        return resolveData;
    }
}

