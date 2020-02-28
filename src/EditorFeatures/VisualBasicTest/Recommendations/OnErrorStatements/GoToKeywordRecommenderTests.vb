' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class GoToKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GoToAfterOnErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>On Error |</MethodBody>, "GoTo")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GoToNotAfterOnErrorInLambdaTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>
Dim x = Sub()
            On Error |
        End Sub</MethodBody>, Array.Empty(Of String)())
        End Function
    End Class
End Namespace
