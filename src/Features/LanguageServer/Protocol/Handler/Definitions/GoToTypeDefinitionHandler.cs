// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentTypeDefinitionName)]
    internal class GoToTypeDefinitionHandler : GoToDefinitionHandlerBase, IRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        public GoToTypeDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService) : base(metadataAsSourceFileService)
        {
        }

        public async Task<LSP.Location[]> HandleRequestAsync(Solution solution, LSP.TextDocumentPositionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            return await GetDefinitionAsync(solution, request, typeOnly: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
