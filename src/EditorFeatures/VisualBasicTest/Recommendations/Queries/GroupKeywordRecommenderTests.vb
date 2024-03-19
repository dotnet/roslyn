' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GroupKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub GroupNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Group")
        End Sub

        <Fact>
        Public Sub GroupInQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Group")
        End Sub

        <Fact>
        Public Sub GroupInQueryAfterGroupIntoTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Group By y Into |</MethodBody>, "Group")
        End Sub

        <Fact>
        Public Sub GroupInQueryAfterAliasedAggregationTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Group By y Into w = |</MethodBody>, "Group")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Sub GroupAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Group")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub GroupAfterAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Group")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        Public Sub GroupAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Group")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543221")>
        Public Sub GroupInsideIntoClauseFollowingAggregateFunctionTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group i1 By i1 = i1 Into Count, |</MethodBody>, "Group")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        Public Sub GroupAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Group")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543240")>
        Public Sub GroupInsideIntoClauseOfGroupJoinTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group Join o1 As Byte In arr On i1 Equals o1 Into |</MethodBody>, "Group")
        End Sub
    End Class
End Namespace
