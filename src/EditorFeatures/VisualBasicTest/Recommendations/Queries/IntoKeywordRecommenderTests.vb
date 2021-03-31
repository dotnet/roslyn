' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class IntoKeywordRecommenderTests
        <WorkItem(543191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543191")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoAfterAnonymousObjectCreationExprTest()
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By i1 = New With {.Key = num} |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Into")
        End Sub

        <WorkItem(543193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543193")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoAfterExprRangeVariableInGroupByTest()
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By num |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Into")
        End Sub

        <WorkItem(543214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543214")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoImmediatelyAfterAnonymousObjectCreationExprTest()
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By i1 = New With {.Key = num}|
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Into")
        End Sub

        <WorkItem(543232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Into")
        End Sub
    End Class
End Namespace
