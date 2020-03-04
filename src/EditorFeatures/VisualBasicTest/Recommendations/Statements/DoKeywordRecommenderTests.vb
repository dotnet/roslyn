' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class DoKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, {"Do", "Do Until", "Do While"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
|</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoAfterExitKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Exit |
Loop</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoAfterContinueKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Continue |
Loop</MethodBody>, "Do")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoNotAfterContinueKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Continue |
</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoNotAfterExitKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Exit |
</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoDoAfterExitInsideLambdaInsideDoLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Do
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoAfterExitInsideDoLoopInsideLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
            Do
                Exit |
            Loop
        End Sub
</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DoNotInsideSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterExitInFinallyBlockTest() As Task
            Dim code =
<MethodBody>
Do
    Try
    Finally
        Exit |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Do")
        End Function
    End Class
End Namespace
