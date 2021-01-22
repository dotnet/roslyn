// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [LspMethod(LSP.Methods.TextDocumentDefinitionName, mutatesSolutionState: false)]
    internal class GoToDefinitionHandler : AbstractGoToDefinitionHandler
    {
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService) : base(metadataAsSourceFileService)
        {
        }

        public override Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
            => GetDefinitionAsync(request, typeOnly: false, context, cancellationToken);
    }
}
