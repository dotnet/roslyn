// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseIsNullCheck;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;

using static SyntaxFactory;
using static UseIsNullCheckHelpers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseIsNullCheckForReferenceEquals), Shared]
internal class CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider
    : AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider<ExpressionSyntax>
{
    private static readonly LiteralExpressionSyntax s_nullLiteralExpression
        = LiteralExpression(SyntaxKind.NullLiteralExpression);

    private static readonly ConstantPatternSyntax s_nullLiteralPattern
        = ConstantPattern(s_nullLiteralExpression);

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider()
    {
    }

    protected override string GetTitle(bool negated, ParseOptions options)
        => UseIsNullCheckHelpers.GetTitle(negated, options);

    private static SyntaxNode CreateEqualsNullCheck(ExpressionSyntax argument)
        => BinaryExpression(SyntaxKind.EqualsExpression, argument, s_nullLiteralExpression).Parenthesize();

    private static SyntaxNode CreateIsNullCheck(ExpressionSyntax argument)
        => IsPatternExpression(argument, s_nullLiteralPattern).Parenthesize();

    private static SyntaxNode CreateIsNotNullCheck(ExpressionSyntax argument)
    {
        if (SupportsIsNotPattern(argument.SyntaxTree.Options))
        {
            return IsPatternExpression(
                argument,
                UnaryPattern(
                    Token(SyntaxKind.NotKeyword),
                    s_nullLiteralPattern)).Parenthesize();
        }
        else
        {
            return BinaryExpression(
                SyntaxKind.IsExpression,
                argument,
                PredefinedType(Token(SyntaxKind.ObjectKeyword))).Parenthesize();
        }
    }

    protected override SyntaxNode CreateNullCheck(ExpressionSyntax argument, bool isUnconstrainedGeneric)
        => isUnconstrainedGeneric
            ? CreateEqualsNullCheck(argument)
            : CreateIsNullCheck(argument);

    protected override SyntaxNode CreateNotNullCheck(ExpressionSyntax argument)
        => CreateIsNotNullCheck(argument);
}
