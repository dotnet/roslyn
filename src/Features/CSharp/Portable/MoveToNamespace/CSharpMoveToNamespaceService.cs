// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.CodeAnalysis.CSharp.MoveToNamespace
{
    [ExportLanguageService(typeof(IMoveToNamespaceService), LanguageNames.CSharp), Shared]
    internal class CSharpMoveToNamespaceService :
        AbstractMoveToNamespaceService<NamespaceDeclarationSyntax, TypeDeclarationSyntax>
    {
        protected override string GetNamespaceName(NamespaceDeclarationSyntax syntax)
            => syntax.Name.ToString();

        protected override string GetNamespaceName(TypeDeclarationSyntax syntax)
        {
            var namespaceDecl = syntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            if (namespaceDecl == null)
            {
                return string.Empty;
            }

            return GetNamespaceName(namespaceDecl);
        }
    }
}
