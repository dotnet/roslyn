' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class IsKeywordRecommenderTests

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IsInCaseClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
                <MethodBody>        
                    Select Case 5
                         Case |
                    End Select
                </MethodBody>, "Is")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoIsKeywordAfterCaseAfterCaseElseTest() As Task
            Await VerifyRecommendationsMissingAsync(
                <MethodBody>
                    Select Case 5
                        Case Else
                            Dim i = 3
                        Case |
                    End Select
                </MethodBody>, "Is")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IsInMiddleCaseClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
                <MethodBody>
                    Select Case 5
                        Case 4, |, Is > 7
                    End Select
                </MethodBody>, "Is")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IsInFinalCaseClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
                <MethodBody>
                    Select Case 5
                        Case 4, Is > 5, |
                    End Select
                </MethodBody>, "Is")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IsInExistingIsClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
                <MethodBody>
                    Select Case 5
                        Case |Is > 5
                    End Select
                </MethodBody>, "Is")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
    Select Case 5
        Case 
|
    End Select
</MethodBody>, "Is")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
    Select Case 5
        Case _
|
    End Select
</MethodBody>, "Is")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
    Select Case 5
        Case _ ' Test
|
    End Select
</MethodBody>, "Is")
        End Function
    End Class
End Namespace
