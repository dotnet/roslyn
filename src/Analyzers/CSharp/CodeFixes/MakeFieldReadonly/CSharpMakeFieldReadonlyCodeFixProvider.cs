// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [ImportingConstructor]
        public CSharpMakeFieldReadonlyCodeFixProvider()
        {
        }

        protected override SyntaxNode GetInitializerNode(VariableDeclaratorSyntax declaration)
            => declaration.Initializer?.Value;

        protected override ImmutableList<VariableDeclaratorSyntax> GetVariableDeclarators(FieldDeclarationSyntax fieldDeclaration)
            => fieldDeclaration.Declaration.Variables.ToImmutableListOrEmpty();
    }
}
