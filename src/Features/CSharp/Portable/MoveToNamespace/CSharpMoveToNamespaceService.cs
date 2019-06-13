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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMoveToNamespaceService(
            [Import(AllowDefault = true)] IMoveToNamespaceOptionsService optionsService)
            : base(optionsService)
        {
        }

        protected override string GetNamespaceName(NamespaceDeclarationSyntax namespaceSyntax)
            => namespaceSyntax.Name.ToString();

        protected override string GetNamespaceName(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            var namespaceDecl = typeDeclarationSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            if (namespaceDecl == null)
            {
                return string.Empty;
            }

            return GetNamespaceName(namespaceDecl);
        }

        protected override bool IsContainedInNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration, int position)
        {
            var namespaceDeclarationStart = namespaceDeclaration.NamespaceKeyword.SpanStart;
            var namespaceDeclarationEnd = namespaceDeclaration.OpenBraceToken.SpanStart;

            return position >= namespaceDeclarationStart &&
                position < namespaceDeclarationEnd;
        }
    }
}
