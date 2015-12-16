' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class EndKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndInMethodBody()
            ' It's always a statement (or at least for now)
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "End")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "End")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndMissingInClassBlock()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "End")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndInSingleLineLambda()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "End")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndNotInSingleLineFunctionLambda()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "End")
        End Sub

        <WorkItem(530599)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndNotOutsideOfMethodBody()
            VerifyRecommendationsMissing(<MethodBody>Class C
 |</MethodBody>, "End")
        End Sub

    End Class
End Namespace
