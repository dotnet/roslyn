' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ResumeKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ResumeNextAfterOnErrorTest()
            VerifyRecommendationsContain(<MethodBody>On Error |</MethodBody>, "Resume Next")
        End Sub

        <Fact>
        Public Sub ResumeInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Resume")
        End Sub

        <Fact>
        Public Sub ResumeNotInLambdaTest()
            ' On Error statements are never allowed within lambdas
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            |
End Sub</MethodBody>, "Resume")
        End Sub
    End Class
End Namespace
