// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeMemberStatic;

namespace Microsoft.CodeAnalysis.CSharp.MakeMemberStatic
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMakeMemberStaticCodeFixProvider)), Shared]
    internal sealed class CSharpMakeMemberStaticCodeFixProvider : AbstractMakeMemberStaticCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                "CS0708" // 'MyMethod': cannot declare instance members in a static class
            );

        protected override bool IsValidMemberNode(SyntaxNode node) =>
            node is MemberDeclarationSyntax ||
            (node.IsKind(SyntaxKind.VariableDeclarator) && node.Ancestors().Any(a => a.IsKind(SyntaxKind.FieldDeclaration) || a.IsKind(SyntaxKind.EventFieldDeclaration)));
    }
}
