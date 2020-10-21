' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ExternalSubFunctionKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterDeclareTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare |</ClassDeclaration>, "Sub")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FunctionAfterDeclareTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare |</ClassDeclaration>, "Function")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAndFunctionAfterDeclareAutoTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Sub", "Function")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAndFunctionAfterDeclareAnsiTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Sub", "Function")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAndFunctionAfterDeclareUnicodeTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Sub", "Function")
        End Function
    End Class
End Namespace
