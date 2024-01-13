' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions

    Friend Class CastOperatorsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Friend Shared ReadOnly PredefinedKeywordList As SyntaxKind() = {
            SyntaxKind.CBoolKeyword,
            SyntaxKind.CByteKeyword,
            SyntaxKind.CCharKeyword,
            SyntaxKind.CDateKeyword,
            SyntaxKind.CDblKeyword,
            SyntaxKind.CDecKeyword,
            SyntaxKind.CIntKeyword,
            SyntaxKind.CLngKeyword,
            SyntaxKind.CObjKeyword,
            SyntaxKind.CSByteKeyword,
            SyntaxKind.CShortKeyword,
            SyntaxKind.CSngKeyword,
            SyntaxKind.CStrKeyword,
            SyntaxKind.CUIntKeyword,
            SyntaxKind.CULngKeyword,
            SyntaxKind.CUShortKeyword}

        Protected Overloads Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsStatementContext Then
                Dim recommendedKeywords As New List(Of RecommendedKeyword)

                For Each keyword In PredefinedKeywordList
                    recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                        keyword,
                        String.Format(VBFeaturesResources._0_function, SyntaxFacts.GetText(keyword)),
                        Glyph.MethodPublic,
                        New PredefinedCastExpressionDocumentation(keyword, context.SemanticModel.Compilation),
                        context.SemanticModel,
                        context.Position))
                Next

                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.CTypeKeyword,
                    VBFeaturesResources.CType_function,
                    Glyph.MethodPublic,
                    New CTypeCastExpressionDocumentation()))

                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.DirectCastKeyword,
                    VBFeaturesResources.DirectCast_function,
                    Glyph.MethodPublic,
                    New DirectCastExpressionDocumentation()))

                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.TryCastKeyword,
                    VBFeaturesResources.TryCast_function,
                    Glyph.MethodPublic,
                    New TryCastExpressionDocumentation()))

                Return recommendedKeywords.ToImmutableArray()
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
