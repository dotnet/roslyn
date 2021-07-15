// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.PullMemberUp
{
    [ExportLanguageService(typeof(IMembersPullerService), LanguageNames.CSharp), Shared]
    internal class CSharpMembersPullerService : AbstractMembersPullerService<UsingDirectiveSyntax, CompilationUnitSyntax, NamespaceDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMembersPullerService()
        {
        }

        protected override SyntaxList<UsingDirectiveSyntax> GetCompilationImports(CompilationUnitSyntax node) => node.Usings;

        protected override SyntaxList<UsingDirectiveSyntax> GetNamespaceImports(NamespaceDeclarationSyntax node) => node.Usings;
    }
}
