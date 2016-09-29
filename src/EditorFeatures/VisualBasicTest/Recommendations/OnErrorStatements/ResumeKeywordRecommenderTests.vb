' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
