' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class FinallyKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Finally")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyInTryBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
|
End Try</MethodBody>, "Finally")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyInCatchBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
Catch ex As Exception
|
End Try</MethodBody>, "Finally")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyNotBeforeCatchBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Try
|
Catch ex As Exception
End Try</MethodBody>, "Finally")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyNotInFinallyBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Finally")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyInTryNestedInCatch1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
        Try
        Catch
            Try ' Type an 'E' on the next line
            |
                Throw
            End Try</MethodBody>, "Finally")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FinallyInTryNestedInCatch2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
        Try
        Catch
            Try ' Type an 'E' on the next line
            Catch
            |
                Throw
            End Try</MethodBody>, "Finally")
        End Function
    End Class
End Namespace
