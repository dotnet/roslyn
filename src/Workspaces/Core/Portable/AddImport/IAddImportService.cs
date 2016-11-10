// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal interface IAddImportService : ILanguageService
    {
        SyntaxNode AddImports(
            SyntaxNode root, SyntaxNode contextLocation, 
            IEnumerable<SyntaxNode> newImports, bool placeSystemNamespaceFirst);

        /// <summary>
        /// Given a context location in a provided syntax tree, returns the appropriate container
        /// that <paramref name="import"/> should be added to.
        /// </summary>
        SyntaxNode GetImportContainer(SyntaxNode root, SyntaxNode contextLocation, SyntaxNode import);
    }

    internal static class IAddImportServiceExtensions
    {
        public static SyntaxNode AddImport(
            this IAddImportService service, SyntaxNode root, SyntaxNode contextLocation, 
            SyntaxNode newImport, bool placeSystemNamespaceFirst)
        {
            return service.AddImports(root, contextLocation,
                SpecializedCollections.SingletonEnumerable(newImport), placeSystemNamespaceFirst);
        }
    }
}