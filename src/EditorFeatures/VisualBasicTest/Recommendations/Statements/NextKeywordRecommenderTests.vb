' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NextKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NextNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextNotInLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextNotAfterStatementTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x
|</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextAfterForStatementTest()
            VerifyRecommendationsContain(<MethodBody>
For i = 1 To 10
|</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextAfterForEachStatementTest()
            VerifyRecommendationsContain(<MethodBody>
For i = 1 To 10
|</MethodBody>, "Next")
        End Sub
    End Class
End Namespace
