' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class SelectKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectInQueryTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z |</MethodBody>, "Select")
        End Function

        <WorkItem(543173)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectAfterMultiLineFunctionLambdaExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Select")
        End Function

        <WorkItem(543174)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectAnonymousObjectCreationExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Select")
        End Function

        <WorkItem(543219)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectAfterIntoClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Select")
        End Function

        <WorkItem(543232)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectAfterNestedAggregateFromClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Select")
        End Function
    End Class
End Namespace
