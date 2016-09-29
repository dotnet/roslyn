' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WhileLoopKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
|</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileAfterExitKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
While
Exit |
Loop</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileAfterContinueKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
While
Continue |
Loop</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileNotAfterContinueKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Continue |
</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileNotAfterExitKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Exit |
</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWhileAfterExitInsideLambdaInsideWhileLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
While
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileAfterExitInsideWhileLoopInsideLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
            While True
                Exit |
            Loop
        End Sub
</MethodBody>, "While")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterExitInFinallyBlockTest() As Task
            Dim code =
<MethodBody>
While True
    Try
    Finally
        Exit |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "While")
        End Function
    End Class
End Namespace
