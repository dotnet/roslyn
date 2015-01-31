' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class WhereKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereNotInStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Where")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereInQuery()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Where")
        End Sub

        <WorkItem(543173)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterMultiLineFunctionLambdaExpr()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Where")
        End Sub

        <WorkItem(543174)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAnonymousObjectCreationExpr()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Where")
        End Sub

        <WorkItem(543219)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterIntoClause()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Where")
        End Sub

        <WorkItem(543232)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterNestedAggregateFromClause()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Where")
        End Sub

        <WorkItem(531545)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereAfterEOL()
            VerifyRecommendationsContain(
<MethodBody>
    Dim q1 = From i4 In arr 
|</MethodBody>, "Where")
        End Sub

        <WorkItem(531545)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereMissingAfterTwoEOL()
            VerifyRecommendationsMissing(
<MethodBody>
    Dim q1 = From i4 In arr 

|</MethodBody>, "Where")
        End Sub

        <WorkItem(531545)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhereMissingAfterTwoEOLWithLineContinuation()
            VerifyRecommendationsMissing(
<MethodBody>
    Dim q1 = From i4 In arr _

|</MethodBody>, "Where")
        End Sub
    End Class
End Namespace
