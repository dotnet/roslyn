' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class CallKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub CallInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Call")
        End Sub

        <Fact>
        Public Sub CallAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "Call")
        End Sub

        <Fact>
        Public Sub CallMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Call")
        End Sub

        <Fact>
        Public Sub CallInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "Call")
        End Sub

        <Fact>
        Public Sub CallNotInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "Call")
        End Sub
    End Class
End Namespace
