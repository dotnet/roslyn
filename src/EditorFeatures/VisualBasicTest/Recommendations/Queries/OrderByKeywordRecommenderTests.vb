' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class OrderByKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub OrderByNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Order By")
        End Sub

        <Fact>
        Public Sub OrderByInQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Order By")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542710")>
        Public Sub OrderByInQueryAfterArrayInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In New Integer() { 4, 5 } |</MethodBody>, "Order By")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Sub OrderByAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Order By")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub OrderByAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Order By")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        Public Sub OrderByAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Order By")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        Public Sub OrderByAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Order By")
        End Sub
    End Class
End Namespace
