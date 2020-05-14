' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class GoToKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GoToInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "GoTo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GoToInMultiLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "GoTo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GoToNotInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub() |</MethodBody>, "GoTo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GoToNotInFinallyBlockTest() As Task
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "GoTo")
        End Function
    End Class
End Namespace
