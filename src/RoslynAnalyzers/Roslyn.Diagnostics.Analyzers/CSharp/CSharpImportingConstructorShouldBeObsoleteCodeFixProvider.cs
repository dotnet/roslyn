// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;

namespace Roslyn.Diagnostics.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpImportingConstructorShouldBeObsoleteCodeFixProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
internal sealed class CSharpImportingConstructorShouldBeObsoleteCodeFixProvider() :
    AbstractImportingConstructorShouldBeObsoleteCodeFixProvider
{
    private protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;
    private protected override SyntaxGeneratorInternal SyntaxGeneratorInternal => CSharpSyntaxGeneratorInternal.Instance;
}
