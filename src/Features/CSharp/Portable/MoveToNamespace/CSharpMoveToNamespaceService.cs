// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MoveToNamespace
{
    [ExportLanguageService(typeof(IMoveToNamespaceService), LanguageNames.CSharp), Shared]
    internal class CSharpMoveToNamespaceService :
        AbstractMoveToNamespaceService<CompilationUnitSyntax, NamespaceDeclarationSyntax, TypeDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMoveToNamespaceService(
            [Import(AllowDefault = true)] IMoveToNamespaceOptionsService optionsService)
            : base(optionsService)
        {
        }

        protected override string GetNamespaceName(SyntaxNode container)
            => container switch
            {
                NamespaceDeclarationSyntax namespaceSyntax => namespaceSyntax.Name.ToString(),
                CompilationUnitSyntax compilationUnit => string.Empty,
                _ => throw ExceptionUtilities.UnexpectedValue(container)
            };

        protected override bool IsContainedInNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration, int position)
        {
            var namespaceDeclarationStart = namespaceDeclaration.NamespaceKeyword.SpanStart;
            var namespaceDeclarationEnd = namespaceDeclaration.OpenBraceToken.SpanStart;

            return position >= namespaceDeclarationStart &&
                position < namespaceDeclarationEnd;
        }
    }
}
