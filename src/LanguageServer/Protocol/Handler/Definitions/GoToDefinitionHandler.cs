// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(GoToDefinitionHandler)), Shared]
    [Method(LSP.Methods.TextDocumentDefinitionName)]
    internal class GoToDefinitionHandler : AbstractGoToDefinitionHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
            : base(metadataAsSourceFileService, globalOptions)
        {
        }

        public override Task<LSP.Location[]?> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
            => GetDefinitionAsync(request, typeOnly: false, context, cancellationToken);
    }
}
