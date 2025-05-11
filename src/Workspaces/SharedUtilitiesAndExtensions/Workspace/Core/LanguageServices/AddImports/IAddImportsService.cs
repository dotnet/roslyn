// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.AddImport;

internal interface IAddImportsService : ILanguageService
{
    AddImportPlacementOptions GetAddImportOptions(IOptionsReader configOptions, bool allowInHiddenRegions);

    /// <summary>
    /// Returns true if the tree already has an existing import syntactically equivalent to
    /// <paramref name="import"/> in scope at <paramref name="contextLocation"/>.  This includes
    /// global imports for VB.
    /// </summary>
    bool HasExistingImport(Compilation compilation, SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import, SyntaxGenerator generator);

    /// <summary>
    /// Given a context location in a provided syntax tree, returns the appropriate container
    /// that <paramref name="import"/> should be added to.
    /// </summary>
    SyntaxNode GetImportContainer(SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import, AddImportPlacementOptions options);

    SyntaxNode AddImports(
        Compilation compilation, SyntaxNode root, SyntaxNode? contextLocation,
        IEnumerable<SyntaxNode> newImports, SyntaxGenerator generator, AddImportPlacementOptions options, CancellationToken cancellationToken);
}

internal static class IAddImportServiceExtensions
{
    public static SyntaxNode AddImport(
        this IAddImportsService service, Compilation compilation, SyntaxNode root,
        SyntaxNode contextLocation, SyntaxNode newImport, SyntaxGenerator generator, AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        return service.AddImports(compilation, root, contextLocation,
            [newImport], generator, options, cancellationToken);
    }
}
