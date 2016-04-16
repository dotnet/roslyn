' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
