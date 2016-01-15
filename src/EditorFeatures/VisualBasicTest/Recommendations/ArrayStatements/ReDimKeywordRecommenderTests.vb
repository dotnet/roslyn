' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    Public Class ReDimKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReDimInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "ReDim")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReDimAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x 
|</MethodBody>, "ReDim")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReDimMissingInClassBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "ReDim")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReDimInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub() |</MethodBody>, "ReDim")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReDimNotInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Function() |</MethodBody>, "ReDim")
        End Function
    End Class
End Namespace
