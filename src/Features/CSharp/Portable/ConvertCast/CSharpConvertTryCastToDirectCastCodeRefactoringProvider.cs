// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertCast;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.ConvertCast;

using static CSharpSyntaxTokens;

/// <summary>
/// Refactor:
///     var o = 1 as object;
///
/// Into:
///     var o = (object)1;
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertTryCastToDirectCast), Shared]
internal partial class CSharpConvertTryCastToDirectCastCodeRefactoringProvider
    : AbstractConvertCastCodeRefactoringProvider<TypeSyntax, BinaryExpressionSyntax, CastExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpConvertTryCastToDirectCastCodeRefactoringProvider()
    {
    }

    protected override string GetTitle()
        => CSharpFeaturesResources.Change_to_cast;

    protected override int FromKind => (int)SyntaxKind.AsExpression;

    protected override TypeSyntax GetTypeNode(BinaryExpressionSyntax expression)
        => (TypeSyntax)expression.Right;

    protected override CastExpressionSyntax ConvertExpression(BinaryExpressionSyntax asExpression, NullableContext nullableContext, bool isReferenceType)
    {
        var expression = asExpression.Left;
        var typeNode = GetTypeNode(asExpression);

        // Trivia handling:
        // #0 exp #1 as #2 Type #3
        // #0 #2 (Type)exp #1 #3
        // Some trivia in the middle (#1 and #2) is moved to the front or behind  the expression
        // #1 and #2 change their position in the expression (#2 goes in front to stay near the type and #1 to the end to stay near the expression)
        var openParen = OpenParenToken;
        var closeParen = CloseParenToken;
        var newTrailingTrivia = asExpression.Left.GetTrailingTrivia().SkipInitialWhitespace().ToSyntaxTriviaList().AddRange(asExpression.GetTrailingTrivia());
        var newLeadingTrivia = asExpression.GetLeadingTrivia().AddRange(asExpression.OperatorToken.TrailingTrivia.SkipInitialWhitespace());
        typeNode = typeNode.WithoutTrailingTrivia();

        // Make sure we make reference type nullable when converting expressions like `null as string` -> `(string?)null`
        if (expression.IsKind(SyntaxKind.NullLiteralExpression) && nullableContext.HasFlag(NullableContext.AnnotationsEnabled) && isReferenceType)
            typeNode = NullableType(typeNode, QuestionToken);

        var castExpression = CastExpression(openParen, typeNode, closeParen, expression.WithoutTrailingTrivia())
            .WithLeadingTrivia(newLeadingTrivia)
            .WithTrailingTrivia(newTrailingTrivia);

        return castExpression;
    }
}
