﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpMakeFieldReadonlyCodeFixProvider()
        {
        }

        protected override SyntaxNode GetInitializerNode(VariableDeclaratorSyntax declaration)
            => declaration.Initializer?.Value;

        protected override ImmutableList<VariableDeclaratorSyntax> GetVariableDeclarators(FieldDeclarationSyntax fieldDeclaration)
            => fieldDeclaration.Declaration.Variables.ToImmutableListOrEmpty();
    }
}
