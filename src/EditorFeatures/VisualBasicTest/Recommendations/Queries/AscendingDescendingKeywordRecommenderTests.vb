' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class AscendingDescendingKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AscendingDescendingNotInStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AscendingDescendingNotInQuery()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y In z |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AscendingDescendingAfterFirstOrderByClause()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Order By y |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AscendingDescendingAfterSecondOrderByClause()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Let w = y Order By y, w |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AscendingDescendingNotAfterAscendingDescending()
            VerifyRecommendationsMissing(<MethodBody>Dim x = From y In z Order By y Ascending |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(542930)>
        Public Sub AscendingDescendingAfterNestedQuery()
            VerifyRecommendationsContain(<MethodBody>Dim x = From y In z Order By From w In z |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(543173)>
        Public Sub AscendingDescendingAfterMultiLineFunctionLambdaExpr()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Ascending", "Descending")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(543174)>
        Public Sub AscendingDescendingAfterAnonymousObjectCreationExpr()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Ascending", "Descending")
        End Sub
    End Class
End Namespace
