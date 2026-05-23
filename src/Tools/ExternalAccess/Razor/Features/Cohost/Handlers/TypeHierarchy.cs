// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;
using RoslynTypeHierarchy = Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class TypeHierarchy
{
    public static Task<LSP.TypeHierarchyItem[]?> PrepareTypeHierarchyAsync(
        Document document, LinePosition linePosition, CancellationToken cancellationToken)
        => RoslynTypeHierarchy.PrepareTypeHierarchyHandler.PrepareTypeHierarchyAsync(document, linePosition, cancellationToken);

    public static Task<LSP.TypeHierarchyItem[]?> ResolveSupertypesAsync(
        Document document, LSP.TypeHierarchyItem item, CancellationToken cancellationToken)
        => RoslynTypeHierarchy.TypeHierarchySupertypesHandler.ResolveSupertypesAsync(document, item, cancellationToken);

    public static Task<LSP.TypeHierarchyItem[]?> ResolveSubtypesAsync(
        Document document, LSP.TypeHierarchyItem item, CancellationToken cancellationToken)
        => RoslynTypeHierarchy.TypeHierarchySubtypesHandler.ResolveSubtypesAsync(document, item, cancellationToken);
}
