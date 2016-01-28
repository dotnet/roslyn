' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class OrderByKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Order By")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByInQueryTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z |</MethodBody>, "Order By")
        End Function

        <WorkItem(542710)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByInQueryAfterArrayInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In New Integer() { 4, 5 } |</MethodBody>, "Order By")
        End Function

        <WorkItem(543173)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByAfterMultiLineFunctionLambdaExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Order By")
        End Function

        <WorkItem(543174)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByAnonymousObjectCreationExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Order By")
        End Function

        <WorkItem(543219)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByAfterIntoClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Order By")
        End Function

        <WorkItem(543232)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OrderByAfterNestedAggregateFromClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Order By")
        End Function
    End Class
End Namespace
