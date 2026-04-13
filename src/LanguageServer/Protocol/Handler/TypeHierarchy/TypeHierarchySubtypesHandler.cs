// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TypeHierarchy;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(TypeHierarchySubtypesHandler)), Shared]
[Method(LSP.Methods.TypeHierarchySubtypesName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TypeHierarchySubtypesHandler() : ILspServiceDocumentRequestHandler<LSP.TypeHierarchySubtypesParams, LSP.TypeHierarchyItem[]?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TypeHierarchySubtypesParams request)
        => TypeHierarchyHelpers.GetResolveData(request.Item).TextDocument;

    public async Task<LSP.TypeHierarchyItem[]?> HandleRequestAsync(LSP.TypeHierarchySubtypesParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var solution = document.Project.Solution;
        var typeSymbol = await TypeHierarchyHelpers.GetTypeSymbolAsync(request.Item, solution, cancellationToken).ConfigureAwait(false);
        if (typeSymbol == null)
            return null;

        var service = document.GetRequiredLanguageService<ITypeHierarchyService>();
        var derivedTypes = await service.GetDerivedTypesAndImplementationsAsync(solution, typeSymbol, transitive: false, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<LSP.TypeHierarchyItem>.GetInstance(out var items);
        foreach (var derivedType in derivedTypes)
        {
            var item = await TypeHierarchyHelpers.CreateItemAsync(derivedType, solution, cancellationToken).ConfigureAwait(false);
            if (item != null)
                items.Add(item);
        }

        return items.ToArray();
    }
}
