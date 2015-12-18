' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class LoopKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Loop")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopNotInLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Loop")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopNotAfterStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x
|</MethodBody>, "Loop")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopAfterDoStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
|</MethodBody>, "Loop", "Loop Until", "Loop While")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopAfterDoUntilStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do Until True
|</MethodBody>, "Loop")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopUntilNotAfterDoUntilStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Do Until True
|</MethodBody>, "Loop Until", "Loop While")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LoopNotInDoLoopUntilBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Do
|
Loop Until True</MethodBody>, "Loop")
        End Function
    End Class
End Namespace
