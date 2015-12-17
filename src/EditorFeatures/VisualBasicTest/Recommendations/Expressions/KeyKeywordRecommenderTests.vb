' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class KeyKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotInStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Key")
        End Sub

        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotAfterArrayInitializerSquiggle()
            VerifyRecommendationsMissing(<MethodBody>Dim x = {|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotAfterArrayInitializerComma()
            VerifyRecommendationsMissing(<MethodBody>Dim x = {0, |</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotAfterAs()
            VerifyRecommendationsMissing(<MethodBody>Dim x As |</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousInitializer1()
            VerifyRecommendationsContain(<MethodBody>Dim x As New With {|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousInitializer2()
            VerifyRecommendationsContain(<MethodBody>Dim x As New With {.Foo = 2, |</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousExpression1()
            VerifyRecommendationsContain(<MethodBody>Dim x = New With {|</MethodBody>, "Key")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyInAnonymousExpression2()
            VerifyRecommendationsContain(<MethodBody>Dim x = New With {.Foo = 2, |</MethodBody>, "Key")
        End Sub

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotInOnymousInitializer()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New Foo With {|</MethodBody>, "Key")
        End Sub

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeyNotInOnymousExpression()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New Foo With {|</MethodBody>, "Key")
        End Sub
    End Class
End Namespace
