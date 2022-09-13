' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class OnErrorKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub OnErrorResumeNextInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "On Error Resume Next")
        End Sub

        <Fact>
        Public Sub OnErrorGoToInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "On Error GoTo")
        End Sub

        <Fact>
        Public Sub OnErrorResumeNextNotInLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() 
            |
End Sub
</MethodBody>, "On Error Resume Next")
        End Sub

        <Fact>
        Public Sub OnErrorGoToNotInLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() 
            |
End Sub
</MethodBody>, "On Error GoTo")
        End Sub
    End Class
End Namespace
