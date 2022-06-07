' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class WhereKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Where")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereInQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Where")
        End Sub

        <WorkItem(543173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Where")
        End Sub

        <WorkItem(543174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Where")
        End Sub

        <WorkItem(543219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Where")
        End Sub

        <WorkItem(543232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Where")
        End Sub

        <WorkItem(531545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531545")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterEOLTest()
            VerifyRecommendationsContain(
<MethodBody>
    Dim q1 = From i4 In arr 
|</MethodBody>, "Where")
        End Sub

        <WorkItem(531545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531545")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereMissingAfterTwoEOLTest()
            VerifyRecommendationsMissing(
<MethodBody>
    Dim q1 = From i4 In arr 

|</MethodBody>, "Where")
        End Sub

        <WorkItem(531545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531545")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereMissingAfterTwoEOLWithLineContinuationTest()
            VerifyRecommendationsMissing(
<MethodBody>
    Dim q1 = From i4 In arr _

|</MethodBody>, "Where")
        End Sub
    End Class
End Namespace
