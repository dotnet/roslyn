' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class GroupKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Group")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupInQueryTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z |</MethodBody>, "Group")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupInQueryAfterGroupIntoTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z Group By y Into |</MethodBody>, "Group")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupInQueryAfterAliasedAggregationTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z Group By y Into w = |</MethodBody>, "Group")
        End Function

        <WorkItem(543173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupAfterMultiLineFunctionLambdaExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Group")
        End Function

        <WorkItem(543174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupAfterAnonymousObjectCreationExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Group")
        End Function

        <WorkItem(543219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupAfterIntoClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Group")
        End Function

        <WorkItem(543221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543221")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupInsideIntoClauseFollowingAggregateFunctionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group i1 By i1 = i1 Into Count, |</MethodBody>, "Group")
        End Function

        <WorkItem(543232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupAfterNestedAggregateFromClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Group")
        End Function

        <WorkItem(543240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543240")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GroupInsideIntoClauseOfGroupJoinTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group Join o1 As Byte In arr On i1 Equals o1 Into |</MethodBody>, "Group")
        End Function
    End Class
End Namespace
