// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.Diagnostics.Analyzers;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpImportingConstructorShouldBeObsoleteCodeFixProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
internal sealed class CSharpImportingConstructorShouldBeObsoleteCodeFixProvider() :
    AbstractImportingConstructorShouldBeObsoleteCodeFixProvider
{
    protected override bool IsPrimaryConstructorTypeDeclaration(SyntaxNode node)
        => node is TypeDeclarationSyntax { ParameterList: not null };

    protected override SyntaxNode MethodTargetingAttributeList(SyntaxNode attribute)
        => AttributeList(AttributeTargetSpecifier(MethodKeyword), [(AttributeSyntax)attribute]);
}
