' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AscendingDescendingKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AscendingDescendingNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact>
        Public Sub AscendingDescendingNotInQueryTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y In z |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact>
        Public Sub AscendingDescendingAfterFirstOrderByClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Order By y |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact>
        Public Sub AscendingDescendingAfterSecondOrderByClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Let w = y Order By y, w |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact>
        Public Sub AscendingDescendingNotAfterAscendingDescendingTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y In z Order By y Ascending |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542930")>
        Public Sub AscendingDescendingAfterNestedQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Order By From w In z |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Sub AscendingDescendingAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub AscendingDescendingAfterAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Ascending", "Descending")
        End Sub
    End Class
End Namespace
