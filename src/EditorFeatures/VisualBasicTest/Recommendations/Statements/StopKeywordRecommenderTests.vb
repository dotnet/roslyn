' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class StopKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub StopInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Stop")
        End Sub

        <Fact>
        Public Sub StopAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "Stop")
        End Sub

        <Fact>
        Public Sub StopMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Stop")
        End Sub

        <Fact>
        Public Sub StopInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "Stop")
        End Sub

        <Fact>
        Public Sub StopNotInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "Stop")
        End Sub
    End Class
End Namespace
