' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    Public Class PreserveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PreserveNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Preserve")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PreserveAfterReDimStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ReDim | </MethodBody>, "Preserve")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PreserveNotAfterReDimPreserveTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReDim Preserve |</ClassDeclaration>, "Preserve")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PreserveNotAfterWeirdBrokenReDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>ReDim x, ReDim |</MethodBody>, "Preserve")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PreserveInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub() ReDim |</MethodBody>, "Preserve")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>ReDim 
| </MethodBody>, "Preserve")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>ReDim _
| </MethodBody>, "Preserve")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>ReDim _ ' Test
| </MethodBody>, "Preserve")
        End Function
    End Class
End Namespace
