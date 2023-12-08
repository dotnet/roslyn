' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GoToKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub GoToInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "GoTo")
        End Sub

        <Fact>
        Public Sub GoToInMultiLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "GoTo")
        End Sub

        <Fact>
        Public Sub GoToNotInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |</MethodBody>, "GoTo")
        End Sub

        <Fact>
        Public Sub GoToNotInFinallyBlockTest()
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            VerifyRecommendationsMissing(code, "GoTo")
        End Sub
    End Class
End Namespace
