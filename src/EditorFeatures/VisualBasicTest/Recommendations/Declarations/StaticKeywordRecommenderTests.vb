' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class StaticKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub StaticInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Static")
        End Sub

        <Fact>
        Public Sub StaticInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Static")
        End Sub

        <Fact>
        Public Sub StaticAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "Static")
        End Sub

        <Fact>
        Public Sub StaticNotInsideSingleLineLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Static")
        End Sub
    End Class
End Namespace
