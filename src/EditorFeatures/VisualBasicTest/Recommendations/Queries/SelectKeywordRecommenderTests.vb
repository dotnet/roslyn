' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class SelectKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub SelectInQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Select")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Sub SelectAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Select")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub SelectAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Select")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        Public Sub SelectAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Select")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        Public Sub SelectAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Select")
        End Sub
    End Class
End Namespace
