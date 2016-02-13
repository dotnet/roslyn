' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class AscendingDescendingKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AscendingDescendingNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AscendingDescendingNotInQueryTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = From y In z |</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AscendingDescendingAfterFirstOrderByClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z Order By y |</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AscendingDescendingAfterSecondOrderByClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z Let w = y Order By y, w |</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AscendingDescendingNotAfterAscendingDescendingTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = From y In z Order By y Ascending |</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(542930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542930")>
        Public Async Function AscendingDescendingAfterNestedQueryTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = From y In z Order By From w In z |</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(543173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Async Function AscendingDescendingAfterMultiLineFunctionLambdaExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Ascending", "Descending")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(543174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Async Function AscendingDescendingAfterAnonymousObjectCreationExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Ascending", "Descending")
        End Function
    End Class
End Namespace
