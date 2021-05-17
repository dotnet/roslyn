' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class TryKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TryInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Try")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TryInMultiLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Try")

        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TryInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Try")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TryInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Function() |
                                         </ClassDeclaration>, "Try")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExitInTryBlockTest()
            Dim code =
<MethodBody>
Try
    Exit |
</MethodBody>

            VerifyRecommendationsContain(code, "Try")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<MethodBody>
Try
Finally
    Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "Try")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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
