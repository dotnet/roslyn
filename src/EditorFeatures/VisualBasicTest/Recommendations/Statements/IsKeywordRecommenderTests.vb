' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class IsKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInCaseClauseTest()
            VerifyRecommendationsContain(
                <MethodBody>        
                    Select Case 5
                         Case |
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoIsKeywordAfterCaseAfterCaseElseTest()
            VerifyRecommendationsMissing(
                <MethodBody>
                    Select Case 5
                        Case Else
                            Dim i = 3
                        Case |
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInMiddleCaseClauseTest()
            VerifyRecommendationsContain(
                <MethodBody>
                    Select Case 5
                        Case 4, |, Is > 7
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInFinalCaseClauseTest()
            VerifyRecommendationsContain(
                <MethodBody>
                    Select Case 5
                        Case 4, Is > 5, |
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInExistingIsClauseTest()
            VerifyRecommendationsContain(
                <MethodBody>
                    Select Case 5
                        Case |Is > 5
                    End Select
                </MethodBody>, "Is")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>
    Select Case 5
        Case 
|
    End Select
</MethodBody>, "Is")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>
    Select Case 5
        Case _
|
    End Select
</MethodBody>, "Is")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
    Select Case 5
        Case _ ' Test
|
    End Select
</MethodBody>, "Is")
        End Sub
    End Class
End Namespace
