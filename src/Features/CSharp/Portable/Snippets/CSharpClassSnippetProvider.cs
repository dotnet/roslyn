// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpClassSnippetProvider()
    : AbstractCSharpTypeSnippetProvider<ClassDeclarationSyntax>(TypeKind.Class)
{
    private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.FileKeyword,
    };

    public override string Identifier => CSharpSnippetIdentifiers.Class;

    public override string Description => FeaturesResources.class_;

    protected override ISet<SyntaxKind> ValidModifiers => s_validModifiers;

    protected override ClassDeclarationSyntax TypeDeclaration(string name)
        => SyntaxFactory.ClassDeclaration(name);
}
