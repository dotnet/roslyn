' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class ResumeKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ResumeNextAfterOnErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>On Error |</MethodBody>, "Resume Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ResumeInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Resume")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ResumeNotInLambdaTest() As Task
            ' On Error statements are never allowed within lambdas
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub()
            |
End Sub</MethodBody>, "Resume")
        End Function
    End Class
End Namespace
