// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveType
{
    [ExportLanguageService(typeof(IMoveTypeService), LanguageNames.CSharp), Shared]
    internal class CSharpMoveTypeService :
        AbstractMoveTypeService<CSharpMoveTypeService, BaseTypeDeclarationSyntax, NamespaceDeclarationSyntax, MemberDeclarationSyntax, CompilationUnitSyntax>
    {
        [ImportingConstructor]
        public CSharpMoveTypeService()
        {
        }
    }
}
