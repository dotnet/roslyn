' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CharsetModifierKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoAfterDeclare()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Auto")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AnsiAfterDeclare()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Ansi")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UnicodeAfterDeclare()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Unicode")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoNotAfterAnotherCharsetModifier1()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Auto")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoNotAfterAnotherCharsetModifier2()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Auto")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoNotAfterAnotherCharsetModifier3()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Auto")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterColon()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare : |</ClassDeclaration>, "Unicode")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare 
 |</ClassDeclaration>, "Unicode")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Sub
    End Class
End Namespace
