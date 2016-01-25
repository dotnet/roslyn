' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class IntoKeywordRecommenderTests
        <WorkItem(543191)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IntoAfterAnonymousObjectCreationExprTest() As Task
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By i1 = New With {.Key = num} |
                         </MethodBody>

            Await VerifyRecommendationsAreExactlyAsync(method, "Into")
        End Function

        <WorkItem(543193)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IntoAfterExprRangeVariableInGroupByTest() As Task
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By num |
                         </MethodBody>

            Await VerifyRecommendationsAreExactlyAsync(method, "Into")
        End Function

        <WorkItem(543214)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IntoImmediatelyAfterAnonymousObjectCreationExprTest() As Task
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By i1 = New With {.Key = num}|
                         </MethodBody>

            Await VerifyRecommendationsAreExactlyAsync(method, "Into")
        End Function

        <WorkItem(543232)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IntoAfterNestedAggregateFromClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Into")
        End Function
    End Class
End Namespace
