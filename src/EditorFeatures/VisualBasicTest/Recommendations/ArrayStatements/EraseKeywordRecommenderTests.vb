' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class EraseKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub EraseInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Erase")
        End Sub

        <Fact>
        Public Sub EraseAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "Erase")
        End Sub

        <Fact>
        Public Sub EraseMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Erase")
        End Sub

        <Fact>
        Public Sub EraseInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "Erase")
        End Sub

        <Fact>
        Public Sub EraseNotInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "Erase")
        End Sub
    End Class
End Namespace
