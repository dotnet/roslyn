// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.InitializeParameter;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter), Shared]
[ExtensionOrder(Before = nameof(CSharpAddParameterCheckCodeRefactoringProvider))]
[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.Wrapping)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpInitializeMemberFromParameterCodeRefactoringProvider() :
    AbstractInitializeMemberFromParameterCodeRefactoringProvider<
        BaseTypeDeclarationSyntax,
        ParameterSyntax,
        StatementSyntax,
        ExpressionSyntax>
{
    protected override bool IsFunctionDeclaration(SyntaxNode node)
        => InitializeParameterHelpers.IsFunctionDeclaration(node);

    protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

    // Fields are always private by default in C#.
    protected override Accessibility DetermineDefaultFieldAccessibility(INamedTypeSymbol containingType)
        => Accessibility.Private;

    // Properties are always private by default in C#.
    protected override Accessibility DetermineDefaultPropertyAccessibility()
        => Accessibility.Private;

    protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
        => InitializeParameterHelpers.GetBody(functionDeclaration);

    protected override SyntaxNode RemoveThrowNotImplemented(SyntaxNode node)
        => InitializeParameterHelpers.RemoveThrowNotImplemented(node);
}
