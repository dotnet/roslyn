' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    Public Class EraseKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EraseInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Erase")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EraseAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x 
|</MethodBody>, "Erase")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EraseMissingInClassBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "Erase")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EraseInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub() |</MethodBody>, "Erase")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EraseNotInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Function() |</MethodBody>, "Erase")
        End Function
    End Class
End Namespace
