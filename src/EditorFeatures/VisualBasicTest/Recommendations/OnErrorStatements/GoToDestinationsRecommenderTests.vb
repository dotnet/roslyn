' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class GoToDestinationsRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ZeroAndOneAfterOnErrorGotoTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>On Error Goto |</MethodBody>, "0", "-1")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>On Error Goto 
|</MethodBody>, "0", "-1")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<MethodBody>On Error Goto _
 |</MethodBody>, "0", "-1")
        End Function
    End Class
End Namespace
