' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideClassDeclaration
        Inherits RecommenderTests

#Region "Scope Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Protected Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Protected Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Public", "Protected", "Friend", "Protected Friend", "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyPublicAfterWideningTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared Widening |</ClassDeclaration>, "Public")
            VerifyRecommendationsMissing(<ClassDeclaration>Shared Widening |</ClassDeclaration>, "Protected", "Friend", "Protected Friend", "Private")
        End Sub

        <WorkItem(545037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545037")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Private")
        End Sub

        <WorkItem(545037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545037")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DefaultNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Default")
        End Sub

        <WorkItem(530330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530330")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Protected", "Protected Friend")
        End Sub

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterAsyncTest()
            VerifyRecommendationsContain(<ClassDeclaration>Async |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterIteratorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Iterator |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Sub

        <WorkItem(20837, "https://github.com/dotnet/roslyn/issues/20837")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExtensionAttribute()
            VerifyRecommendationsContain(<ClassDeclaration>&lt;Extension&gt; |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Sub

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterProtectedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterProtectedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Widening")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Widening")
        End Sub

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritAfterPartialTest()
            VerifyRecommendationsContain(<ClassDeclaration>Partial |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableAfterPartialTest()
            VerifyRecommendationsContain(<ClassDeclaration>Partial |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "NotInheritable")
        End Sub

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overrides")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "MustOverride")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overridable")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "NotOverridable")
        End Sub

        ' ---------

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overloads")
        End Sub

        <WorkItem(530330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530330")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Overridable", "NotOverridable", "MustOverride")
        End Sub

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "WriteOnly")
        End Sub

#End Region

#Region "Partial Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterMustInheritTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterNotInheritableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Partial")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeywordsAfterPartialTest()
            VerifyRecommendationsAreExactly(
                <ClassDeclaration>Partial |</ClassDeclaration>,
                "Class", "Interface", "MustInherit", "NotInheritable", "Overloads", "Private", "Shadows", "Structure", "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeywordsAfterPartialPrivateTest()
            VerifyRecommendationsAreExactly(
                <ClassDeclaration>Partial Private |</ClassDeclaration>,
                "Class", "Interface", "MustInherit", "NotInheritable", "Overloads", "Shadows", "Structure", "Sub")
        End Sub

#End Region

#Region "Shadows Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterReadOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterWriteOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterNarrowingTest()
            VerifyRecommendationsContain(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterWideningTest()
            VerifyRecommendationsContain(<ClassDeclaration>Widening |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Shadows")
        End Sub

#End Region

#Region "Shared Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesExistTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Shared")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesNotExistAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Shared")
        End Sub

        <WorkItem(545039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Shared")
        End Sub

        <WorkItem(545039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesDoesNotExistAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Overrides")
        End Sub

        <WorkItem(545039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Shadows")
        End Sub

#End Region

    End Class
End Namespace
