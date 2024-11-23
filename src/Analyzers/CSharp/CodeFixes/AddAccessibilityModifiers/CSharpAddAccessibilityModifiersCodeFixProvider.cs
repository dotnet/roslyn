// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAccessibilityModifiers), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpAddAccessibilityModifiersCodeFixProvider() : AbstractAddAccessibilityModifiersCodeFixProvider
{
    protected override SyntaxNode MapToDeclarator(SyntaxNode node)
    {
        switch (node)
        {
            case FieldDeclarationSyntax field:
                return field.Declaration.Variables[0];

            case EventFieldDeclarationSyntax eventField:
                return eventField.Declaration.Variables[0];

            default:
                return node;
        }
    }
}
