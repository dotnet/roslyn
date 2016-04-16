' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ElseIfKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfNotInMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfInMultiLineIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
|
End If</MethodBody>, "ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfInMultiLineElseIf1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
ElseIf True Then
|
End If</MethodBody>, "ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfInMultiLineElseIf2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
Else If True Then
|
End If</MethodBody>, "ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfNotInMultiLineElseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then
Else 
|
End If</MethodBody>, "ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfNotInSingleLineIf1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then |</MethodBody>, "ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ElseIfNotInSingleLineIf2Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>If True Then Stop Else |</MethodBody>, "ElseIf")
        End Function
    End Class
End Namespace
