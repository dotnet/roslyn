' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ForKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
|</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForAfterExitKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
For
Exit |
Loop</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForAfterContinueKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
For
Continue |
Loop</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForNotAfterContinueKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Continue |
</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForNotAfterExitKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Exit |
</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForNotInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Sub() |</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoForAfterExitInsideLambdaInsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
For Each i In foo
    x = Sub()
            Exit |
        End Sub
Next
</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForAfterExitInsideForLoopInsideLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
            For Each i in bar
                Exit |
        End Function
        Next
</MethodBody>, "For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ForNotAfterExitInsideForLoopInsideFinallyBlockTest() As Task
            Dim code =
<MethodBody>
For i = 1 to 100
    Try
    Finally
        Exit |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "For")
        End Function
    End Class
End Namespace
