' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class FinallyKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Finally")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInTryBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
|
End Try</MethodBody>, "Finally")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInCatchBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch ex As Exception
|
End Try</MethodBody>, "Finally")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyNotBeforeCatchBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Try
|
Catch ex As Exception
End Try</MethodBody>, "Finally")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyNotInFinallyBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Finally")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInTryNestedInCatch1Test()
            VerifyRecommendationsContain(<MethodBody>
        Try
        Catch
            Try ' Type an 'E' on the next line
            |
                Throw
            End Try</MethodBody>, "Finally")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInTryNestedInCatch2Test()
            VerifyRecommendationsContain(<MethodBody>
        Try
        Catch
            Try ' Type an 'E' on the next line
            Catch
            |
                Throw
            End Try</MethodBody>, "Finally")
        End Sub
    End Class
End Namespace
