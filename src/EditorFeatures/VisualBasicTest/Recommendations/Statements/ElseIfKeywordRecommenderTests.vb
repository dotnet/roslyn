' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ElseIfKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ElseIfNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        Public Sub ElseIfInMultiLineIfTest()
            VerifyRecommendationsContain(<MethodBody>If True Then
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        Public Sub ElseIfInMultiLineElseIf1Test()
            VerifyRecommendationsContain(<MethodBody>If True Then
ElseIf True Then
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        Public Sub ElseIfInMultiLineElseIf2Test()
            VerifyRecommendationsContain(<MethodBody>If True Then
Else If True Then
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        Public Sub ElseIfNotInMultiLineElseTest()
            VerifyRecommendationsMissing(<MethodBody>If True Then
Else 
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        Public Sub ElseIfNotInSingleLineIf1Test()
            VerifyRecommendationsMissing(<MethodBody>If True Then |</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        Public Sub ElseIfNotInSingleLineIf2Test()
            VerifyRecommendationsMissing(<MethodBody>If True Then Stop Else |</MethodBody>, "ElseIf")
        End Sub
    End Class
End Namespace
