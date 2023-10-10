// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(MapCodeHandler)), Shared]
[Method(LSP.MapperMethods.TextDocumentMapCodeName)]
internal sealed partial class MapCodeHandler : ILspServiceDocumentRequestHandler<MapCodeParams, LSP.WorkspaceEdit?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MapCodeHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;


    public TextDocumentIdentifier GetTextDocumentIdentifier(MapCodeParams request)
        => request.TextDocument;

    public Task<WorkspaceEdit?> HandleRequestAsync(MapCodeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Document);
        Contract.ThrowIfNull(context.Solution);

        return Task.FromResult<WorkspaceEdit?>(null);
    }
}
