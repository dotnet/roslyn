' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class TryKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub TryInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Try")
        End Sub

        <Fact>
        Public Sub TryInMultiLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Try")

        End Sub

        <Fact>
        Public Sub TryInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Try")
        End Sub

        <Fact>
        Public Sub TryInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Function() |
                                         </ClassDeclaration>, "Try")
        End Sub

        <Fact>
        Public Sub AfterExitInTryBlockTest()
            Dim code =
<MethodBody>
Try
    Exit |
</MethodBody>

            VerifyRecommendationsContain(code, "Try")
        End Sub

        <Fact>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<MethodBody>
Try
Finally
    Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "Try")
        End Sub

        <Fact>
        Public Sub AfterExitInCatchBlockTest()
            Dim code =
<MethodBody>
Try
Catch
    Exit |
</MethodBody>

            VerifyRecommendationsContain(code, "Try")
        End Sub
    End Class
End Namespace
