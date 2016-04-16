' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class KeyKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Key")
        End Function

        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyNotAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = {|</MethodBody>, "Key")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyNotAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = {0, |</MethodBody>, "Key")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyNotAfterAsTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x As |</MethodBody>, "Key")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyInAnonymousInitializer1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x As New With {|</MethodBody>, "Key")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyInAnonymousInitializer2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x As New With {.Foo = 2, |</MethodBody>, "Key")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyInAnonymousExpression1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = New With {|</MethodBody>, "Key")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyInAnonymousExpression2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = New With {.Foo = 2, |</MethodBody>, "Key")
        End Function

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyNotInOnymousInitializerTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x As New Foo With {|</MethodBody>, "Key")
        End Function

        ''' <remark>Yes, "Onymous" is a word.</remark>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeyNotInOnymousExpressionTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = New Foo With {|</MethodBody>, "Key")
        End Function
    End Class
End Namespace
