// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(PrepareTypeHierarchyHandler)), Shared]
[Method(LSP.Methods.PrepareTypeHierarchyName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PrepareTypeHierarchyHandler() : ILspServiceDocumentRequestHandler<LSP.TypeHierarchyPrepareParams, LSP.TypeHierarchyItem[]?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TypeHierarchyPrepareParams request)
        => request.TextDocument;

    public async Task<LSP.TypeHierarchyItem[]?> HandleRequestAsync(LSP.TypeHierarchyPrepareParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var solution = document.Project.Solution;
        var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, solution.Services, includeType: true, cancellationToken).ConfigureAwait(false);
        var typeSymbol = GetTargetTypeSymbol(symbol);
        if (typeSymbol == null)
            return null;

        var item = await TypeHierarchyHelpers.CreateItemAsync(typeSymbol, solution, preferredDocumentId: document.Id, cancellationToken).ConfigureAwait(false);
        return item == null ? null : [item];
    }

    private static INamedTypeSymbol? GetTargetTypeSymbol(ISymbol? symbol)
        => symbol switch
        {
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol,
            IMethodSymbol methodSymbol when methodSymbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor => methodSymbol.ContainingType,
            { ContainingType: not null } containingTypeSymbol => containingTypeSymbol.ContainingType,
            _ => null,
        };
}
