' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class IfKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfInMultiLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub() |</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterElseInMultiLine1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
If True Then
Else |
End If</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterElseInMultiLine2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
If True Then
Else If
Else |
End If</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterElseInSingleLineIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then Stop Else |</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterExternalSourceDirectiveTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
#ExternalSource ("file", 1)
|
#End ExternalSource
</MethodBody>, "If")
        End Function
    End Class
End Namespace
