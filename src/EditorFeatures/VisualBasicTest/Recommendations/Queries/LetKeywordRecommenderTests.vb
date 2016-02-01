' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class LetKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LetNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Let")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LetInQueryTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z |</MethodBody>, "Let")
        End Function

        <WorkItem(543085, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543085")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LetAfterLambdaInQueryTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From num In numbers Let n6 As Func(Of Integer) = Function() 5 |</MethodBody>, "Let")
        End Function

        <WorkItem(543173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LetAfterMultiLineFunctionLambdaExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Let")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(543174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Async Function LetAfterAnonymousObjectCreationExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Let")
        End Function

        <WorkItem(543219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LetAfterIntoClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Let")
        End Function

        <WorkItem(543232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LetAfterNestedAggregateFromClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Let")
        End Function
    End Class
End Namespace
