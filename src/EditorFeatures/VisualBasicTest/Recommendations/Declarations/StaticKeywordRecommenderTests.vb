' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class StaticKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StaticInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Static")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StaticInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Static")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StaticAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
|</MethodBody>, "Static")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StaticNotInsideSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Static")
        End Function
    End Class
End Namespace
