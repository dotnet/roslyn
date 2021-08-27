' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideModuleDeclaration

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DefaultNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Default")
        End Sub

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Narrowing")
        End Sub

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Overloads")
        End Sub

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Overrides")
        End Sub

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Shadows")
        End Sub

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Shared")
        End Sub

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Widening")
        End Sub

        <Fact>
        <WorkItem(554103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554103")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Partial")
        End Sub

        <Fact>
        <WorkItem(554103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554103")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterPrivateTest()
            VerifyRecommendationsContain(<ModuleDeclaration>Private |</ModuleDeclaration>, "Partial")
        End Sub
    End Class
End Namespace
