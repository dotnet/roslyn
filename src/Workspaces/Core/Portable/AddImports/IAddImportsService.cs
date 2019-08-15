// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImports
{
    internal interface IAddImportsService : ILanguageService
    {
        /// <summary>
        /// Returns true if the tree already has an existing import syntactically equivalent to
        /// <paramref name="import"/> in scope at <paramref name="contextLocation"/>.  This includes
        /// global imports for VB.
        /// </summary>
        bool HasExistingImport(Compilation compilation, SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import);

        /// <summary>
        /// Given a context location in a provided syntax tree, returns the appropriate container
        /// that <paramref name="import"/> should be added to.
        /// </summary>
        SyntaxNode GetImportContainer(SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import);

        SyntaxNode AddImports(
            Compilation compilation, SyntaxNode root, SyntaxNode? contextLocation,
            IEnumerable<SyntaxNode> newImports, bool placeSystemNamespaceFirst);
    }

    internal static class IAddImportServiceExtensions
    {
        public static SyntaxNode AddImport(
            this IAddImportsService service, Compilation compilation, SyntaxNode root,
            SyntaxNode contextLocation, SyntaxNode newImport, bool placeSystemNamespaceFirst)
        {
            return service.AddImports(compilation, root, contextLocation,
                SpecializedCollections.SingletonEnumerable(newImport), placeSystemNamespaceFirst);
        }
    }
}
