' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InInForEach1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For Each x |</MethodBody>, "In")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InInForEach2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For Each x As Goo |</MethodBody>, "In")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InInFromQuery1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From x |</MethodBody>, "In")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InInFromQuery2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From x As Goo |</MethodBody>, "In")
        End Function

        <WorkItem(543231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543231")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InInFromQuery3Test() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>Dim x = From x As Integer |</MethodBody>, "In")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>For Each x 
|</MethodBody>, "In")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>For Each x _
|</MethodBody>, "In")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>For Each x _ ' Test
|</MethodBody>, "In")
        End Function
    End Class
End Namespace
