' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideStructureDeclaration

#Region "Scope Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PublicExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Public")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedMissingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>|</StructureDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PrivateExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedFriendMissingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>|</StructureDeclaration>, "Protected Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PublicNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Public |</StructureDeclaration>, "Public")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Public |</StructureDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PrivateNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Public |</StructureDeclaration>, "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Public |</StructureDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedFriendNotAfterPublicTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Public |</StructureDeclaration>, "Protected Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendNotAfterProtectedTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected |</StructureDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedNotAfterFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Friend |</StructureDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Protected")
        End Function

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Widening |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterProtectedTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterProtectedTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterPrivateTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Private |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterPrivateTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Private |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "Widening")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "Widening")
        End Function

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Widening |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Widening |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "NotInheritable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "NotInheritable")
        End Function

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overrides")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideMissingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>|</StructureDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "MustOverride")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableMissingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>|</StructureDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overridable")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableMissingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>|</StructureDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "NotOverridable")
        End Function

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overloads")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overloads")
        End Function

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Default |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Default |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "WriteOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "WriteOnly")
        End Function

#End Region

#Region "Partial Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Partial |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialAfterMustInheritTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialAfterNotInheritableTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Widening |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "Partial")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Default |</StructureDeclaration>, "Partial")
        End Function

#End Region

#Region "Shadows Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsExistsTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overloads |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overrides |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Overridable |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterReadOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterNarrowingTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterWideningTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Widening |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsNotAfterShadowsTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shadows |</StructureDeclaration>, "Shadows")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>Default |</StructureDeclaration>, "Shadows")
        End Function

#End Region

#Region "Shared Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Shared")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedDoesNotExistAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<StructureDeclaration>Shared |</StructureDeclaration>, "Shared")
        End Function

#End Region

    End Class
End Namespace
