// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.InvertLogical;

namespace Microsoft.CodeAnalysis.CSharp.InvertLogical;

[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertLogical), Shared]
internal class CSharpInvertLogicalCodeRefactoringProvider :
    AbstractInvertLogicalCodeRefactoringProvider<SyntaxKind, ExpressionSyntax, BinaryExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpInvertLogicalCodeRefactoringProvider()
    {
    }

    protected override string GetOperatorText(SyntaxKind binaryExprKind)
        => binaryExprKind == SyntaxKind.LogicalAndExpression
            ? SyntaxFacts.GetText(SyntaxKind.AmpersandAmpersandToken)
            : SyntaxFacts.GetText(SyntaxKind.BarBarToken);
}
