// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpAddAccessibilityModifiersCodeFixProvider : AbstractAddAccessibilityModifiersCodeFixProvider
    {
        protected override SyntaxNode MapFieldDeclaration(SyntaxNode node)
        {
            var declarator = (VariableDeclaratorSyntax)node;
            var declaration = (VariableDeclarationSyntax)declarator.Parent;
            var field = (FieldDeclarationSyntax)declaration.Parent;

            return field;
        }
    }
}
