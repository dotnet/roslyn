// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.AddAwait;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddAwait;

/// <summary>
/// This refactoring complements the AddAwait fixer. It allows adding `await` and `await ... .ConfigureAwait(false)` even there is no compiler error to trigger the fixer.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddAwait), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpAddAwaitCodeRefactoringProvider()
    : AbstractAddAwaitCodeRefactoringProvider<ExpressionSyntax>
{
    protected override string GetTitle()
        => CSharpCodeFixesResources.Add_await;

    protected override string GetTitleWithConfigureAwait()
        => CSharpFeaturesResources.Add_await_and_ConfigureAwaitFalse;

    protected override bool IsInAsyncContext(SyntaxNode node)
    {
        foreach (var current in node.Ancestors())
        {
            switch (current)
            {
                case AnonymousFunctionExpressionSyntax anonymousFunction:
                    return anonymousFunction.AsyncKeyword != default;
                case MethodDeclarationSyntax methodDeclaration:
                    return methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword);
            }
        }

        return false;
    }
}
