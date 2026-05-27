// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(PrepareCallHierarchyHandler)), Shared]
[Method(LSP.Methods.PrepareCallHierarchyName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PrepareCallHierarchyHandler() : ILspServiceDocumentRequestHandler<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CallHierarchyPrepareParams request)
        => request.TextDocument;

    public async Task<LSP.CallHierarchyItem[]?> HandleRequestAsync(LSP.CallHierarchyPrepareParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var solution = document.Project.Solution;
        var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, solution.Services, includeType: true, cancellationToken).ConfigureAwait(false);
        if (symbol == null)
            return null;

        var service = document.GetRequiredLanguageService<ICallHierarchyService>();
        var itemDescriptor = await service.CreateItemAsync(symbol, document.Project, cancellationToken).ConfigureAwait(false);
        if (itemDescriptor == null)
            return null;

        var item = await CallHierarchyHelpers.CreateItemAsync(itemDescriptor, solution, preferredDocumentId: document.Id, cancellationToken).ConfigureAwait(false);
        return item == null ? null : [item];
    }
}
