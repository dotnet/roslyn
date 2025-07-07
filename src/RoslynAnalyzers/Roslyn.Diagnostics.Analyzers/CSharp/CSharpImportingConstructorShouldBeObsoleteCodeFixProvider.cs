// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.Diagnostics.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(CSharpImportingConstructorShouldBeObsoleteCodeFixProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
internal sealed class CSharpImportingConstructorShouldBeObsoleteCodeFixProvider : ImportingConstructorShouldBeObsoleteCodeFixProvider
{
    protected override SyntaxNode RetargetAttributeToMethod(SyntaxNode obsoleteAttribute)
        => ((AttributeSyntax)obsoleteAttribute).With
}
