// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Roslyn.Diagnostics.Analyzers;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpExportedPartsShouldHaveImportingConstructorCodeFixProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
internal sealed class CSharpExportedPartsShouldHaveImportingConstructorCodeFixProvider() :
    AbstractExportedPartsShouldHaveImportingConstructorCodeFixProvider
{
    protected override bool IsOnPrimaryConstructorTypeDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
    {
        typeDeclaration = node as TypeDeclarationSyntax;
        return typeDeclaration is TypeDeclarationSyntax { ParameterList: not null };
    }

    protected override SyntaxNode AddMethodTarget(SyntaxNode attributeList)
        => ((AttributeListSyntax)attributeList).WithTarget(AttributeTargetSpecifier(MethodKeyword));
}
