' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class CatchKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Catch")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchInTryBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
|
Finally
End Try</MethodBody>, "Catch")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchInCatchBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
Catch ex As Exception
|
Finally
End Try</MethodBody>, "Catch")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CatchNotInFinallyBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Catch")
        End Function
    End Class
End Namespace
