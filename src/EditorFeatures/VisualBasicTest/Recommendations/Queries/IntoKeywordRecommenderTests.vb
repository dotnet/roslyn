' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class IntoKeywordRecommenderTests
        <WorkItem(543191)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoAfterAnonymousObjectCreationExpr()
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By i1 = New With {.Key = num} |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Into")
        End Sub

        <WorkItem(543193)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoAfterExprRangeVariableInGroupBy()
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By num |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Into")
        End Sub

        <WorkItem(543214)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoImmediatelyAfterAnonymousObjectCreationExpr()
            Dim method = <MethodBody>
                            Dim q1 = From num In New Integer() {4, 5} Group By i1 = New With {.Key = num}|
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Into")
        End Sub

        <WorkItem(543232)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IntoAfterNestedAggregateFromClause()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Into")
        End Sub
    End Class
End Namespace
