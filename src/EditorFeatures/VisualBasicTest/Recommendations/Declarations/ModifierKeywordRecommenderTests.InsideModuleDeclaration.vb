' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideModuleDeclaration

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DefaultNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Default")
        End Function

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Narrowing")
        End Function

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Overloads")
        End Function

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Overrides")
        End Function

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Shadows")
        End Function

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Shared")
        End Function

        <Fact>
        <WorkItem(544630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Widening")
        End Function

        <Fact>
        <WorkItem(554103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554103")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Partial")
        End Function

        <Fact>
        <WorkItem(554103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554103")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialAfterPrivateTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>Private |</ModuleDeclaration>, "Partial")
        End Function
    End Class
End Namespace
