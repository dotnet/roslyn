// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    internal class GoToDefinitionHandler : AbstractGoToDefinitionHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, ILspSolutionProvider solutionProvider)
            : base(metadataAsSourceFileService, solutionProvider)
        {
        }

        public override Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
            => GetDefinitionAsync(request, typeOnly: false, context, cancellationToken);
    }
}
