// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SpellCheck;

namespace Microsoft.CodeAnalysis.CSharp.SpellCheck;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SpellCheck), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
internal sealed partial class CSharpSpellCheckCodeFixProvider : AbstractSpellCheckCodeFixProvider<SimpleNameSyntax>
{
    private const string CS0426 = nameof(CS0426); // The type name '0' does not exist in the type '1'
    private const string CS1520 = nameof(CS1520); // Method must have a return type

    private const string CS0539 = nameof(CS0539); // error CS0539: 'A.Goo<T>()' in explicit interface declaration is not a member of interface

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpSpellCheckCodeFixProvider()
    {
    }

    private static ImmutableArray<string> GetFixableDiagnosticIds()
    {
        var generateMethodFixable = GenerateMethodDiagnosticIds.FixableDiagnosticIds.Except([CS0539]);
        return
        [
            .. AddImportDiagnosticIds.FixableDiagnosticIds,
            .. generateMethodFixable,
            CS0426,
            CS1520,
        ];
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = GetFixableDiagnosticIds();

    protected override bool ShouldSpellCheck(SimpleNameSyntax name)
        => !name.IsVar;

    protected override bool DescendIntoChildren(SyntaxNode arg)
    {
        // Don't dive into type argument lists.  We don't want to report spell checking
        // fixes for type args when we're called on an outer generic type.
        return arg is not TypeArgumentListSyntax;
    }

    protected override bool IsGeneric(SyntaxToken token)
        => token.GetNextToken().Kind() == SyntaxKind.LessThanToken;

    protected override bool IsGeneric(SimpleNameSyntax nameNode)
        => nameNode is GenericNameSyntax;

    protected override bool IsGeneric(CompletionItem completionItem)
        => completionItem.DisplayTextSuffix == "<>";

    protected override SyntaxToken CreateIdentifier(SyntaxToken nameToken, string newName)
        => SyntaxFactory.Identifier(newName).WithTriviaFrom(nameToken);
}
