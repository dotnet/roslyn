' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ExternalSubFunctionKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterDeclare()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Sub")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterDeclare()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAndFunctionAfterDeclareAuto()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Sub", "Function")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAndFunctionAfterDeclareAnsi()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Sub", "Function")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAndFunctionAfterDeclareUnicode()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Sub", "Function")
        End Sub
    End Class
End Namespace
