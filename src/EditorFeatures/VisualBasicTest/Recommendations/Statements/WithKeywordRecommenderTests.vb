' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WithKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
|</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithNotAfterExitKeywordTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
With
Exit |
Loop</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithNotAfterContinueKeywordTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
With
Continue |
Loop</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithNotAfterContinueKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Continue |
</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithNotAfterExitKeywordOutsideLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Exit |
</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithNotAfterExitInsideLambdaInsideWithBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
While
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterExitInsideWhileLoopInsideLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub()
            With x
                Exit |
            Loop
        End Sub
</MethodBody>, "With")
        End Function
    End Class
End Namespace
