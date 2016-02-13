' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class NextKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextNotInLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextNotAfterStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x
|</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextAfterForStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
For i = 1 To 10
|</MethodBody>, "Next")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NextAfterForEachStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
For i = 1 To 10
|</MethodBody>, "Next")
        End Function
    End Class
End Namespace
