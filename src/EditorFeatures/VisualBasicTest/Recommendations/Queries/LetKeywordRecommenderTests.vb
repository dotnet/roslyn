' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class LetKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LetNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Let")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LetInQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Let")
        End Sub

        <WorkItem(543085, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543085")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LetAfterLambdaInQueryTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From num In numbers Let n6 As Func(Of Integer) = Function() 5 |</MethodBody>, "Let")
        End Sub

        <WorkItem(543173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LetAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Let")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(543174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub LetAfterAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Let")
        End Sub

        <WorkItem(543219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LetAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Let")
        End Sub

        <WorkItem(543232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LetAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Let")
        End Sub
    End Class
End Namespace
