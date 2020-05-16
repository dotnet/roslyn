// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentDefinitionName)]
    internal class GoToDefinitionHandler : GoToDefinitionHandlerBase, IRequestHandler<LSP.TextDocumentPositionParams, object>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService) : base(metadataAsSourceFileService)
        {
        }

        public async Task<object> HandleRequestAsync(Solution solution, LSP.TextDocumentPositionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            return await GetDefinitionAsync(solution, request, typeOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
