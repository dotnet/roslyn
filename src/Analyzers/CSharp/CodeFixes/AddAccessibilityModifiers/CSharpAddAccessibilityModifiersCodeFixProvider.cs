// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddOrRemoveAccessibilityModifiers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddOrRemoveAccessibilityModifiers), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpAddOrRemoveAccessibilityModifiersCodeFixProvider() : AbstractAddOrRemoveAccessibilityModifiersCodeFixProvider
{
    protected override SyntaxNode MapToDeclarator(SyntaxNode node)
        => node is BaseFieldDeclarationSyntax field ? field.Declaration.Variables[0] : node;
}
