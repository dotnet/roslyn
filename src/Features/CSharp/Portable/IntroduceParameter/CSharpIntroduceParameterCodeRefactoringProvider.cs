// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceParameter;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceParameter;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceParameter), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpIntroduceParameterCodeRefactoringProvider()
    : AbstractIntroduceParameterCodeRefactoringProvider<
    ExpressionSyntax,
    InvocationExpressionSyntax,
    ObjectCreationExpressionSyntax,
    IdentifierNameSyntax,
    ArgumentSyntax>
{
    protected override SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol)
    {
        return ExpressionGenerator.GenerateExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference: true);
    }

    protected override SyntaxNode? GetLocalDeclarationFromDeclarator(SyntaxNode variableDecl)
    {
        return variableDecl.Parent?.Parent as LocalDeclarationStatementSyntax;
    }

    protected override bool IsDestructor(IMethodSymbol methodSymbol)
    {
        return false;
    }

    protected override SyntaxNode UpdateArgumentListSyntax(SyntaxNode argumentList, SeparatedSyntaxList<ArgumentSyntax> arguments)
        => ((ArgumentListSyntax)argumentList).WithArguments(arguments);
}
