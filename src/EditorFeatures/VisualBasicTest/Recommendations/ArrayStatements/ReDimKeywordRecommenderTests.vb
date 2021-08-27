﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    Public Class ReDimKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "ReDim")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReDimNotInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "ReDim")
        End Sub
    End Class
End Namespace
