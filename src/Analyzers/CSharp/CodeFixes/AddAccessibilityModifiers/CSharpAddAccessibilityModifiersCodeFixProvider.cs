// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpAddAccessibilityModifiersCodeFixProvider : AbstractAddAccessibilityModifiersCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
