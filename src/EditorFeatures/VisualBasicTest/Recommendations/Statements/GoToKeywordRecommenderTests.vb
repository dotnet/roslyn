' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
