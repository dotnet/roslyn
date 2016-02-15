' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideClassDeclaration

#Region "Scope Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PublicExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Public")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PrivateExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedFriendExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Protected Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PublicNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Public")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PrivateNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedFriendNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Protected Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendAfterProtectedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedAfterFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Public", "Protected", "Friend", "Protected Friend", "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyPublicAfterWideningTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared Widening |</ClassDeclaration>, "Public")
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared Widening |</ClassDeclaration>, "Protected", "Friend", "Protected Friend", "Private")
        End Function

        <WorkItem(545037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545037")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PrivateNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Private")
        End Function

        <WorkItem(545037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545037")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DefaultNotAfterPrivateTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Default")
        End Function

        <WorkItem(530330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530330")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Protected", "Protected Friend")
        End Function

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterAsyncTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterIteratorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Iterator |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Function

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterProtectedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterProtectedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterPrivateTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterPrivateTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Widening")
        End Function

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "NotInheritable")
        End Function

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesAfterNotOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overrides")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideAfterShadowsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "MustOverride")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableAfterShadowsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overridable")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "NotOverridable")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsAfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsAfterNotOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overloads")
        End Function

        <WorkItem(530330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530330")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Overridable", "NotOverridable", "MustOverride")
        End Function

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterNotOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterNotOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "WriteOnly")
        End Function

#End Region

#Region "Partial Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialAfterMustInheritTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialAfterNotInheritableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeywordsAfterPartialTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(
                <ClassDeclaration>Partial |</ClassDeclaration>,
                "Class", "Interface", "MustInherit", "NotInheritable", "Overloads", "Private", "Shadows", "Structure", "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeywordsAfterPartialPrivateTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(
                <ClassDeclaration>Partial Private |</ClassDeclaration>,
                "Class", "Interface", "MustInherit", "NotInheritable", "Overloads", "Shadows", "Structure", "Sub")
        End Function

#End Region

#Region "Shadows Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterReadOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterNarrowingTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterWideningTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Shadows")
        End Function

#End Region

#Region "Shared Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Shared")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedDoesNotExistAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Shared")
        End Function

        <WorkItem(545039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedAfterShadowsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Shared")
        End Function

        <WorkItem(545039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesDoesNotExistAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Overrides")
        End Function

        <WorkItem(545039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Shadows")
        End Function

#End Region

    End Class
End Namespace
