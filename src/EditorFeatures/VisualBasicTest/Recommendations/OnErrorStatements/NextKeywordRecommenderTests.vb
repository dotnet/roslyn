' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class NextKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextAfterOnErrorResumeTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>On Error Resume |</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextAfterResumeStatementTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Resume |</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextNotInLambdaAfterResumeTest() As Task
            ' On Error statements are never allowed within lambdas
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub()
            Resume |
End Sub</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextNotInLambdaAfterOnErrorResumeTest() As Task
            ' On Error statements are never allowed within lambdas
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub()
            On Error Resume |
End Sub</MethodBody>, "Next")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>On Error Resume 
|</MethodBody>, "Next")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>On Error Resume _
|</MethodBody>, "Next")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>On Error Resume _ ' Test
|</MethodBody>, "Next")
        End Function
    End Class
End Namespace
