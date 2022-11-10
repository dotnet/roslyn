' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ExternalSubFunctionKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub SubAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Sub")
        End Sub

        <Fact>
        Public Sub FunctionAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Function")
        End Sub

        <Fact>
        Public Sub SubAndFunctionAfterDeclareAutoTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Sub", "Function")
        End Sub

        <Fact>
        Public Sub SubAndFunctionAfterDeclareAnsiTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Sub", "Function")
        End Sub

        <Fact>
        Public Sub SubAndFunctionAfterDeclareUnicodeTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Sub", "Function")
        End Sub
    End Class
End Namespace
