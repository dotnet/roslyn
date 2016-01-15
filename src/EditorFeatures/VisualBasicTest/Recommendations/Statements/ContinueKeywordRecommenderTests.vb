' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ContinueKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInForLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
For i = 1 To 10
|
Next</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInForEachLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
For Each i In j
|
Next</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInWhileLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
While True
|
End While</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInDoWhileLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do While True
|
Loop</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInLoopWhileLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
|
Loop While True</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInInfiniteDoWhileLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
|
Loop</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueNotInLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Do
Dim x = Function()
|
        End Function
Loop</MethodBody>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueInClassDeclarationLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Dim _x = Function()
             Do
             |
             Loop
         End Function
</ClassDeclaration>, "Continue")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ContinueNotInSingleLineLambdaInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Continue")
        End Function
    End Class
End Namespace
