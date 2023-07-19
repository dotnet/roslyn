' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class InsideStructureDeclaration
        Inherits RecommenderTests

#Region "Scope Keywords"

        <Fact>
        Public Sub PublicExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Public")
        End Sub

        <Fact>
        Public Sub ProtectedMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub PrivateExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Private")
        End Sub

        <Fact>
        Public Sub FriendExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub ProtectedFriendMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Protected Friend")
        End Sub

        <Fact>
        Public Sub PublicNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Public")
        End Sub

        <Fact>
        Public Sub ProtectedNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub PrivateNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Private")
        End Sub

        <Fact>
        Public Sub FriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub ProtectedFriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Protected Friend")
        End Sub

        <Fact>
        Public Sub FriendNotAfterProtectedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub FriendNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub ProtectedNotAfterFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Friend |</StructureDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub ProtectedNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Protected")
        End Sub

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact>
        Public Sub NarrowingExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterProtectedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterProtectedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterPrivateTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Private |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterPrivateTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Private |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "Widening")
        End Sub

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact>
        Public Sub MustInheritExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritAfterPartialTest()
            VerifyRecommendationsContain(<StructureDeclaration>Partial |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableAfterPartialTest()
            VerifyRecommendationsContain(<StructureDeclaration>Partial |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterSharedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterSharedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "NotInheritable")
        End Sub

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact>
        Public Sub OverridesExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overrides")
        End Sub

        ' ---------

        <Fact>
        Public Sub MustOverrideMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "MustOverride")
        End Sub

        ' ---------

        <Fact>
        Public Sub OverridableMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overridable")
        End Sub

        ' ---------

        <Fact>
        Public Sub NotOverridableMissingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "NotOverridable")
        End Sub

        ' ---------

        <Fact>
        Public Sub OverloadsExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsAfterOverridesTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overloads")
        End Sub

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact>
        Public Sub ReadOnlyExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterSharedTest()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "WriteOnly")
        End Sub

#End Region

#Region "Partial Keyword"

        <Fact>
        Public Sub PartialExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterPartialTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialAfterMustInheritTest()
            VerifyRecommendationsContain(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialAfterNotInheritableTest()
            VerifyRecommendationsContain(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterWideningTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterDefaultTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Default |</StructureDeclaration>, "Partial")
        End Sub

#End Region

#Region "Shadows Keyword"

        <Fact>
        Public Sub ShadowsExistsTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterNotOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterOverridesTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterOverridableTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterReadOnlyTest()
            VerifyRecommendationsContain(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterWriteOnlyTest()
            VerifyRecommendationsContain(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterNarrowingTest()
            VerifyRecommendationsContain(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterWideningTest()
            VerifyRecommendationsContain(<StructureDeclaration>Widening |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterDefaultTest()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "Shadows")
        End Sub

#End Region

#Region "Shared Keyword"

        <Fact>
        Public Sub SharedDoesExistTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Shared")
        End Sub

        <Fact>
        Public Sub SharedDoesNotExistAfterSharedTest()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "Shared")
        End Sub

#End Region

    End Class
End Namespace
