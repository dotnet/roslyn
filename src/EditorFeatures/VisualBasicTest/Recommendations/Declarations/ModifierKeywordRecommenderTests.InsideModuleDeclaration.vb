' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideModuleDeclaration

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DefaultNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Default")
        End Sub

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Narrowing")
        End Sub

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Overloads")
        End Sub

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Overrides")
        End Sub

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Shadows")
        End Sub

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Shared")
        End Sub

        <WpfFact>
        <WorkItem(544630)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Widening")
        End Sub

        <WpfFact>
        <WorkItem(554103)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Partial")
        End Sub

        <WpfFact>
        <WorkItem(554103)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterPrivate()
            VerifyRecommendationsContain(<ModuleDeclaration>Private |</ModuleDeclaration>, "Partial")
        End Sub

    End Class
End Namespace
