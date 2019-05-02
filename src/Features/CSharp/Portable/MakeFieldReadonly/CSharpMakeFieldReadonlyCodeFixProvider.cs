// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeFieldReadonly;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeFieldReadonlyCodeFixProvider : AbstractMakeFieldReadonlyCodeFixProvider<VariableDeclaratorSyntax, FieldDeclarationSyntax>
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpMakeFieldReadonlyCodeFixProvider()
        {
        }

        protected override SyntaxNode GetInitializerNode(VariableDeclaratorSyntax declaration)
            => declaration.Initializer?.Value;

        protected override ImmutableList<VariableDeclaratorSyntax> GetVariableDeclarators(FieldDeclarationSyntax fieldDeclaration)
            => fieldDeclaration.Declaration.Variables.ToImmutableListOrEmpty();
    }
}
