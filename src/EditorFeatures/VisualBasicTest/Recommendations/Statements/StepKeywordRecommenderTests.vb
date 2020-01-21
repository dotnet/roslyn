' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class StepKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StepInForLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For i = 1 To 10 |</MethodBody>, "Step")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StepInForLoopAfterLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
    For i = 1 To 10 _
_
|</MethodBody>, "Step")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StepInForLoopAfterLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
    For i = 1 To 10 _ ' Test
_
|</MethodBody>, "Step")
        End Function
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StepInForLoopNotAfterEOLTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
    For i = 1 To 10 
|</MethodBody>, "Step")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StepInForLoopNotAfterEOLWithLineContinuationTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
    For i = 1 To 10 _

|</MethodBody>, "Step")
        End Function
    End Class
End Namespace
