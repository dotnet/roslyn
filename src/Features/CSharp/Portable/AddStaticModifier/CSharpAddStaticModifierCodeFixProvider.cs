// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddStaticModifier;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddStaticModifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpAddStaticModifierCodeFixProvider)), Shared]
    internal sealed class CSharpAddStaticModifierCodeFixProvider : AbstractAddStaticModifierCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                "CS0708" // 'MyMethod': cannot declare instance members in a static class
            );

        [ImportingConstructor]
        public CSharpAddStaticModifierCodeFixProvider()
        {
        }

        protected override SyntaxNode MapToDeclarator(SyntaxNode node)
        {
            return node switch
            {
                FieldDeclarationSyntax field => field.Declaration.Variables[0],
                EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables[0],
                _ => node,
            };
        }
    }
}
