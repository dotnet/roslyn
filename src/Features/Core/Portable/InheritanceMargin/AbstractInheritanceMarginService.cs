// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InheritanceMargin;

internal abstract partial class AbstractInheritanceMarginService : IInheritanceMarginService
{
    /// <summary>
    /// Given the syntax nodes to search,
    /// get all the method, event, property and type declaration syntax nodes.
    /// </summary>
    protected abstract ImmutableArray<SyntaxNode> GetMembers(IEnumerable<SyntaxNode> nodesToSearch);

    /// <summary>
    /// Get the token that represents declaration node.
    /// e.g. Identifier for method/property/event and this keyword for indexer.
    /// </summary>
    protected abstract SyntaxToken GetDeclarationToken(SyntaxNode declarationNode);

    protected abstract string GlobalImportsTitle { get; }

    public async ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMemberItemsAsync(
        Document document,
        TextSpan spanToSearch,
        bool includeGlobalImports,
        bool frozenPartialSemantics,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var remoteClient = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (remoteClient != null)
        {
            // Also, make it clear to the remote side that they should be using frozen semantics, just like we are.
            // we want results quickly, without waiting for the entire source generator pass to run.  The user will still get
            // accurate results in the future because taggers are set to recompute when compilations are fully
            // available on the OOP side.
            var result = await remoteClient.TryInvokeAsync<IRemoteInheritanceMarginService, ImmutableArray<InheritanceMarginItem>>(
                solution,
                (service, solutionInfo, cancellationToken) =>
                    service.GetInheritanceMarginItemsAsync(solutionInfo, document.Id, spanToSearch, includeGlobalImports, frozenPartialSemantics, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return [];
            }

            return result.Value;
        }
        else
        {
            return await GetInheritanceMarginItemsInProcessAsync(
                document,
                spanToSearch,
                includeGlobalImports,
                frozenPartialSemantics,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool CanHaveInheritanceTarget(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol namedType)
        {
            return !symbol.IsStatic && namedType.TypeKind is TypeKind.Interface or TypeKind.Class or TypeKind.Struct;
        }

        if (symbol is IEventSymbol or IPropertySymbol
            or IMethodSymbol
            {
                MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.UserDefinedOperator or MethodKind.Conversion
            })
        {
            return true;
        }

        return false;
    }
}
