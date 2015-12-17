' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    Public Class ReDimKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimMissingInClassBlock()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimInSingleLineLambda()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimNotInSingleLineFunctionLambda()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "ReDim")
        End Sub
    End Class
End Namespace
