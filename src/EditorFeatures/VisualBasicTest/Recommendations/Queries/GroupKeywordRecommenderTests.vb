' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class GroupKeywordRecommenderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupNotInStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Group")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupInQuery()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z |</MethodBody>, "Group")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupInQueryAfterGroupInto()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Group By y Into |</MethodBody>, "Group")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupInQueryAfterAliasedAggregation()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Group By y Into w = |</MethodBody>, "Group")
        End Sub

        <WorkItem(543173)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupAfterMultiLineFunctionLambdaExpr()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Group")
        End Sub

        <WorkItem(543174)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupAfterAnonymousObjectCreationExpr()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Group")
        End Sub

        <WorkItem(543219)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupAfterIntoClause()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Group")
        End Sub

        <WorkItem(543221)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupInsideIntoClauseFollowingAggregateFunction()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group i1 By i1 = i1 Into Count, |</MethodBody>, "Group")
        End Sub

        <WorkItem(543232)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupAfterNestedAggregateFromClause()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Group")
        End Sub

        <WorkItem(543240)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GroupInsideIntoClauseOfGroupJoin()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group Join o1 As Byte In arr On i1 Equals o1 Into |</MethodBody>, "Group")
        End Sub
    End Class
End Namespace
