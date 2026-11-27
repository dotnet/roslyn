// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CallHierarchy;

internal sealed record CallHierarchyItemId(string SymbolKeyData, ProjectId ProjectId)
{
    public static CallHierarchyItemId Create(ISymbol symbol, Project project, CancellationToken cancellationToken)
        => new(SymbolKey.CreateString(symbol, cancellationToken), project.Id);

    public static bool TryCreate(
        ISymbol symbol,
        Project project,
        CancellationToken cancellationToken,
        [NotNullWhen(true)]
        out CallHierarchyItemId? itemId)
    {
        if (!SymbolKey.CanCreate(symbol, cancellationToken))
        {
            itemId = null;
            return false;
        }

        itemId = Create(symbol, project, cancellationToken);
        return true;
    }

    public async Task<(ISymbol Symbol, Project Project)?> TryResolveAsync(Solution solution, CancellationToken cancellationToken)
    {
        var project = solution.GetProject(ProjectId);
        if (project == null)
            return null;

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation == null)
            return null;

        var symbol = SymbolKey.ResolveString(SymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();
        return symbol == null ? null : (symbol, project);
    }
}
