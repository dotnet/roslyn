' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Protected Overloads Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext Then
                Dim recommendedKeywords As New List(Of RecommendedKeyword)

                For Each keyword In PredefinedKeywordList
                    recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                        keyword,
                        String.Format(VBFeaturesResources.Function1, SyntaxFacts.GetText(keyword)),
                        Glyph.MethodPublic,
                        New PredefinedCastExpressionDocumentation(keyword, context.SemanticModel.Compilation),
                        context.SemanticModel,
                        context.Position))
                Next

                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.CTypeKeyword,
                    VBFeaturesResources.CtypeFunction,
                    Glyph.MethodPublic,
                    New CTypeCastExpressionDocumentation()))

                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.DirectCastKeyword,
                    VBFeaturesResources.DirectcastFunction,
                    Glyph.MethodPublic,
                    New DirectCastExpressionDocumentation()))

                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.TryCastKeyword,
                    VBFeaturesResources.TrycastFunction,
                    Glyph.MethodPublic,
                    New TryCastExpressionDocumentation()))

                Return recommendedKeywords
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
