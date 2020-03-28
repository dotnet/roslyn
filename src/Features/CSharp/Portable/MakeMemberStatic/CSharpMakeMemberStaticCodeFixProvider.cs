// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeMemberStatic;

namespace Microsoft.CodeAnalysis.CSharp.MakeMemberStatic
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMakeMemberStaticCodeFixProvider)), Shared]
    internal sealed class CSharpMakeMemberStaticCodeFixProvider : AbstractMakeMemberStaticCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpMakeMemberStaticCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                "CS0708" // 'MyMethod': cannot declare instance members in a static class
            );

        protected override bool IsValidMemberNode(SyntaxNode node) =>
            node is MemberDeclarationSyntax ||
            (node.IsKind(SyntaxKind.VariableDeclarator) && node.Ancestors().Any(a => a.IsKind(SyntaxKind.FieldDeclaration) || a.IsKind(SyntaxKind.EventFieldDeclaration)));
    }
}
