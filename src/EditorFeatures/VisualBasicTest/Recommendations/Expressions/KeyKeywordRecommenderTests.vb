' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class KeyKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = {|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotAfterArrayInitializerCommaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = {0, |</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotAfterAsTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As |</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousInitializer1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x As New With {|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousInitializer2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x As New With {.Goo = 2, |</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousExpression1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = New With {|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousExpression2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = New With {.Goo = 2, |</MethodBody>, "Key")
        End Sub

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotInOnymousInitializerTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Goo With {|</MethodBody>, "Key")
        End Sub

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotInOnymousExpressionTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Goo With {|</MethodBody>, "Key")
        End Sub
    End Class
End Namespace
