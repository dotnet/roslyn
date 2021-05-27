' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideStructureDeclaration

#Region "Scope Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Protected Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Protected Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterProtectedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Friend |</StructureDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Protected")
        End Sub

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterProtectedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterProtectedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterPrivateTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Private |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterPrivateTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Private |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "Widening")
        End Sub

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritAfterPartialTest()
            VerifyRecommendationsContain(<StructureDeclaration>Partial |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableAfterPartialTest()
            VerifyRecommendationsContain(<StructureDeclaration>Partial |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterSharedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterSharedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "NotInheritable")
        End Sub

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overrides")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "MustOverride")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overridable")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "NotOverridable")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsAfterOverridesTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overloads")
        End Sub

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "WriteOnly")
        End Sub

#End Region

#Region "Partial Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterMustInheritTest()
            VerifyRecommendationsContain(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterNotInheritableTest()
            VerifyRecommendationsContain(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterDefaultTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Default |</StructureDeclaration>, "Partial")
        End Sub

#End Region

#Region "Shadows Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterReadOnlyTest()
            VerifyRecommendationsContain(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterWriteOnlyTest()
            VerifyRecommendationsContain(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterNarrowingTest()
            VerifyRecommendationsContain(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterWideningTest()
            VerifyRecommendationsContain(<StructureDeclaration>Widening |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterDefaultTest()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "Shadows")
        End Sub

#End Region

#Region "Shared Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesExistTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Shared")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesNotExistAfterSharedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "Shared")
        End Sub

#End Region

    End Class
End Namespace
