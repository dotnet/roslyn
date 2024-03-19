// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString), Shared]
internal partial class CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider :
    AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
        ExpressionSyntax,
        LiteralExpressionSyntax,
        InvocationExpressionSyntax,
        InterpolatedStringExpressionSyntax,
        ArgumentSyntax,
        ArgumentListSyntax,
        InterpolationSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider()
    {
    }

    protected override ExpressionSyntax ParseExpression(string text)
        => SyntaxFactory.ParseExpression(text);
}
