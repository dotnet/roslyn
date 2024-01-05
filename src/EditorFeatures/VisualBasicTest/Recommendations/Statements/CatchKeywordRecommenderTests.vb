' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class CatchKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub CatchNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Catch")
        End Sub

        <Fact>
        Public Sub CatchInTryBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
|
Finally
End Try</MethodBody>, "Catch")
        End Sub

        <Fact>
        Public Sub CatchInCatchBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch ex As Exception
|
Finally
End Try</MethodBody>, "Catch")
        End Sub

        <Fact>
        Public Sub CatchNotInFinallyBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Catch")
        End Sub
    End Class
End Namespace
