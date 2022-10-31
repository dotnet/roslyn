' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class IfKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub IfInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfInMultiLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub() |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterElseInMultiLine1Test()
            VerifyRecommendationsContain(<MethodBody>
If True Then
Else |
End If</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterElseInMultiLine2Test()
            VerifyRecommendationsContain(<MethodBody>
If True Then
Else If
Else |
End If</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterElseInSingleLineIfTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Stop Else |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterExternalSourceDirectiveTest()
            VerifyRecommendationsContain(
<MethodBody>
#ExternalSource ("file", 1)
|
#End ExternalSource
</MethodBody>, "If")
        End Sub
    End Class
End Namespace
