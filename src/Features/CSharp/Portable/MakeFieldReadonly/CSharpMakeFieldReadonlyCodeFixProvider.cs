// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeFieldReadonly;

namespace Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeFieldReadonlyCodeFixProvider : AbstractMakeFieldReadonlyCodeFixProvider<FieldDeclarationSyntax, VariableDeclaratorSyntax>
    {
        protected override SyntaxNode GetInitializerNode(VariableDeclaratorSyntax declaration)
            => declaration.Initializer?.Value;

        protected override int GetVariableDeclaratorCount(FieldDeclarationSyntax fieldDeclaration)
            => fieldDeclaration.Declaration.Variables.Count;
    }
}
