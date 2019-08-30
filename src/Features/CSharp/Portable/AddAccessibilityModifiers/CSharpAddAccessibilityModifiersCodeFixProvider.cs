// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpAddAccessibilityModifiersCodeFixProvider : AbstractAddAccessibilityModifiersCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpAddAccessibilityModifiersCodeFixProvider()
        {
        }

        protected override SyntaxNode MapToDeclarator(SyntaxNode node)
        {
            switch (node)
            {
                case FieldDeclarationSyntax field:
                    return field.Declaration.Variables[0];

                case EventFieldDeclarationSyntax eventField:
                    return eventField.Declaration.Variables[0];

                default:
                    return node;
            }
        }
    }
}
