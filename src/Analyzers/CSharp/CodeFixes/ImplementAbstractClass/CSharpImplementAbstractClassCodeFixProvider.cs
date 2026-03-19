// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementAbstractClass;

namespace Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementAbstractClass), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateType)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpImplementAbstractClassCodeFixProvider()
    : AbstractImplementAbstractClassCodeFixProvider<TypeDeclarationSyntax>(CS0534)
{
    private const string CS0534 = nameof(CS0534); // 'Program' does not implement inherited abstract member 'Goo.bar()'

    protected override SyntaxToken GetClassIdentifier(TypeDeclarationSyntax classNode)
        => classNode.Identifier;
}
