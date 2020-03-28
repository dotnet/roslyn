' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class CatchKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Catch")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchInTryBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
|
Finally
End Try</MethodBody>, "Catch")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchInCatchBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
Catch ex As Exception
|
Finally
End Try</MethodBody>, "Catch")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchNotInFinallyBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Catch")
        End Function
    End Class
End Namespace
