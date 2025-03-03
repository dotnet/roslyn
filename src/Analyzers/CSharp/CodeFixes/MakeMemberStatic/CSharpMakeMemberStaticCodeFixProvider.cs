// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MakeMemberStatic;

namespace Microsoft.CodeAnalysis.CSharp.MakeMemberStatic;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMemberStatic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMakeMemberStaticCodeFixProvider() : AbstractMakeMemberStaticCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["CS0708"];

    protected override bool TryGetMemberDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? memberDeclaration)
    {
        if (node is MemberDeclarationSyntax)
        {
            memberDeclaration = node;
            return true;
        }

        if (node.IsKind(SyntaxKind.VariableDeclarator) && node.Parent is VariableDeclarationSyntax { Parent: FieldDeclarationSyntax or EventFieldDeclarationSyntax })
        {
            memberDeclaration = node.Parent.Parent;
            return true;
        }

        memberDeclaration = null;
        return false;
    }
}
