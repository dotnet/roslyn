// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.CodeAnalysis.CSharp.MoveToNamespace
{
    [ExportLanguageService(typeof(AbstractMoveToNamespaceService), LanguageNames.CSharp), Shared]
    internal class CSharpMoveToNamespaceService :
        AbstractMoveToNamespaceService<CompilationUnitSyntax, NamespaceDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMoveToNamespaceService(IMoveToNamespaceOptionsService moveToNamespaceOptionsService)
            : base(moveToNamespaceOptionsService)
        {
        }

        protected override string GetNamespaceName(NamespaceDeclarationSyntax syntax)
        {
            return syntax.Name.ToString();
        }
    }
}
