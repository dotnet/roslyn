' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class UntilAndWhileKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function UntilAfterDoTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do |</MethodBody>, "Until")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileAfterDoTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do |</MethodBody>, "While")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function UntilAfterLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop |</MethodBody>, "Until")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhileAfterLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop |</MethodBody>, "While")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function UntilAndWhileMissingInDoLoopTopTestBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Do Until True
Loop |</MethodBody>, "While", "Until")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function UntilAndWhileMissingAfterInvalidLoopTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Loop |</MethodBody>, "While", "Until")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>Do 
|</MethodBody>, "Until")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>Do _
|</MethodBody>, "Until")
        End Function
    End Class
End Namespace
