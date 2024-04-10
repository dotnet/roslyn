' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class YieldKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub InMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Yield")
        End Sub

        <Fact>
        Public Sub InLambdaBodyTest()
            Dim code =
<MethodBody>
Dim f = Function()
            |
        End Function
</MethodBody>

            VerifyRecommendationsContain(code, "Yield")
        End Sub

        <Fact>
        Public Sub NotInExpressionTest()
            Dim code =
<MethodBody>
Dim f = |
</MethodBody>

            VerifyRecommendationsMissing(code, "Yield")
        End Sub
    End Class
End Namespace
