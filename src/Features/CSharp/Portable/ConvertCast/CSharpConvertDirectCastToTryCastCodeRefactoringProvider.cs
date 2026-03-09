// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertCast;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertCast;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

/// <summary>
/// Refactor:
///     var o = (object)1;
///
/// Into:
///     var o = 1 as object;
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertDirectCastToTryCast), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpConvertDirectCastToTryCastCodeRefactoringProvider()
    : AbstractConvertCastCodeRefactoringProvider<TypeSyntax, CastExpressionSyntax, BinaryExpressionSyntax>
{
    protected override string GetTitle()
        => CSharpFeaturesResources.Change_to_as_expression;

    protected override int FromKind => (int)SyntaxKind.CastExpression;

    protected override TypeSyntax GetTypeNode(CastExpressionSyntax from)
        => from.Type;

    protected override BinaryExpressionSyntax ConvertExpression(CastExpressionSyntax castExpression, NullableContext nullableContext, bool isReferenceType)
    {
        var newTypeNode = castExpression.Type;

        // Cannot use nullable reference types in `as` expression
        // This check ensures we unwrap any nullables, e.g.
        // `(string?)null` -> `null as string`
        if (newTypeNode is NullableTypeSyntax nullableType && isReferenceType)
            newTypeNode = nullableType.ElementType;

        // (T)expr   -->   expr as T

        // 'expr' is moving to front.  Move the existing leading trivia to it, and follow it with a <space> before the 'as'.
        var newExpression = castExpression.Expression
            .WithLeadingTrivia(GetCommentTrivia(castExpression.CloseParenToken.TrailingTrivia))
            .WithPrependedLeadingTrivia(castExpression.GetLeadingTrivia())
            .WithTrailingTrivia(Space);

        // 'as' is in the middle.  Ensure it has no elastic trivia, is followed by a space, and includes any random
        // comments that might have come before the 'T' in the cast.
        var newAsKeyword = AsKeyword
            .WithoutTrivia()
            .WithTrailingTrivia(Space)
            .WithAppendedTrailingTrivia(GetCommentTrivia(castExpression.OpenParenToken.TrailingTrivia));

        // 'T' is moving to the end.  Move the existing trailing trivia to it, and ensure it has no leading trivia.
        newTypeNode = newTypeNode
            .WithoutTrivia()
            .WithAppendedTrailingTrivia(GetCommentTrivia(newTypeNode.GetTrailingTrivia()))
            .WithAppendedTrailingTrivia(castExpression.GetTrailingTrivia());

        return BinaryExpression(SyntaxKind.AsExpression, newExpression, newAsKeyword, newTypeNode);

        // If there is a comment in the trivia, keep the whole list.  Otherwise, return nothing.
        static SyntaxTriviaList GetCommentTrivia(SyntaxTriviaList triviaList)
            => triviaList.Any(t => t.IsRegularComment()) ? triviaList : [];
    }
}
