' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class EachKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EachNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Each")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EachAfterForKeywordTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For |</MethodBody>, "Each")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EachNotAfterTouchingForTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>For|</MethodBody>, "Each")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EachTouchingLoopIdentifierTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For i|</MethodBody>, "Each")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>For 
|</MethodBody>, "Each")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>For _
|</MethodBody>, "Each")
        End Function

        <WorkItem(4946, "http://github.com/dotnet/roslyn/issues/4946")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInForLoop() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<MethodBody>For | = 1 To 100
Next</MethodBody>, {})
        End Function
    End Class
End Namespace
