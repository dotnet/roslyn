' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class KeyKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub KeyNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyNotAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = {|</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyNotAfterArrayInitializerCommaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = {0, |</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyNotAfterAsTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As |</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyInAnonymousInitializer1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x As New With {|</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyInAnonymousInitializer2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x As New With {.Goo = 2, |</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyInAnonymousExpression1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = New With {|</MethodBody>, "Key")
        End Sub

        <Fact>
        Public Sub KeyInAnonymousExpression2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = New With {.Goo = 2, |</MethodBody>, "Key")
        End Sub

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        Public Sub KeyNotInOnymousInitializerTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Goo With {|</MethodBody>, "Key")
        End Sub

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        Public Sub KeyNotInOnymousExpressionTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Goo With {|</MethodBody>, "Key")
        End Sub
    End Class
End Namespace
