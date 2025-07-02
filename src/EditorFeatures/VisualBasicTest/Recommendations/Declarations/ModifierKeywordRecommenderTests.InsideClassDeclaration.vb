' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class InsideClassDeclaration
        Inherits RecommenderTests

#Region "Scope Keywords"

        <Fact>
        Public Sub PublicExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Public")
        End Sub

        <Fact>
        Public Sub ProtectedExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub PrivateExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Private")
        End Sub

        <Fact>
        Public Sub FriendExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub ProtectedFriendExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Protected Friend")
        End Sub

        <Fact>
        Public Sub PublicNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Public")
        End Sub

        <Fact>
        Public Sub ProtectedNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub PrivateNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Private")
        End Sub

        <Fact>
        Public Sub FriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub ProtectedFriendNotAfterPublicTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Protected Friend")
        End Sub

        <Fact>
        Public Sub FriendAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub FriendNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Friend")
        End Sub

        <Fact>
        Public Sub ProtectedAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub ProtectedNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Protected")
        End Sub

        <Fact>
        Public Sub AfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Public", "Protected", "Friend", "Protected Friend", "Private")
        End Sub

        <Fact>
        Public Sub OnlyPublicAfterWideningTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared Widening |</ClassDeclaration>, "Public")
            VerifyRecommendationsMissing(<ClassDeclaration>Shared Widening |</ClassDeclaration>, "Protected", "Friend", "Protected Friend", "Private")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545037")>
        Public Sub PrivateNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Private")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545037")>
        Public Sub DefaultNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Default")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530330")>
        Public Sub AfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Protected", "Protected Friend")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        Public Sub AfterAsyncTest()
            VerifyRecommendationsContain(<ClassDeclaration>Async |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Sub

        <Fact>
        Public Sub AfterIteratorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Iterator |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20837")>
        Public Sub AfterExtensionAttribute()
            VerifyRecommendationsContain(<ClassDeclaration>&lt;Extension&gt; |</ClassDeclaration>, "Public", "Protected", "Protected Friend", "Friend", "Private")
        End Sub

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact>
        Public Sub NarrowingExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterProtectedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterProtectedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Widening")
        End Sub

        <Fact>
        Public Sub NarrowingAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Narrowing")
        End Sub

        <Fact>
        Public Sub WideningAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Widening")
        End Sub

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact>
        Public Sub MustInheritExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritAfterPartialTest()
            VerifyRecommendationsContain(<ClassDeclaration>Partial |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableAfterPartialTest()
            VerifyRecommendationsContain(<ClassDeclaration>Partial |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "NotInheritable")
        End Sub

        <Fact>
        Public Sub MustInheritNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "MustInherit")
        End Sub

        <Fact>
        Public Sub NotInheritableNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "NotInheritable")
        End Sub

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact>
        Public Sub OverridesExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact>
        Public Sub OverridesAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overrides")
        End Sub

        ' ---------

        <Fact>
        Public Sub MustOverrideExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "MustOverride")
        End Sub

        <Fact>
        Public Sub MustOverrideAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "MustOverride")
        End Sub

        ' ---------

        <Fact>
        Public Sub OverridableExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overridable")
        End Sub

        <Fact>
        Public Sub OverridableAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overridable")
        End Sub

        ' ---------

        <Fact>
        Public Sub NotOverridableExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "NotOverridable")
        End Sub

        <Fact>
        Public Sub NotOverridableAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "NotOverridable")
        End Sub

        ' ---------

        <Fact>
        Public Sub OverloadsExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact>
        Public Sub OverloadsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Overloads")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530330")>
        Public Sub OverridableAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Overridable", "NotOverridable", "MustOverride")
        End Sub

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact>
        Public Sub ReadOnlyExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "WriteOnly")
        End Sub

        <Fact>
        Public Sub ReadOnlyAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "ReadOnly")
        End Sub

        <Fact>
        Public Sub WriteOnlyAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "WriteOnly")
        End Sub

#End Region

#Region "Partial Keyword"

        <Fact>
        Public Sub PartialExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialAfterMustInheritTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialAfterNotInheritableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub PartialNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Partial")
        End Sub

        <Fact>
        Public Sub KeywordsAfterPartialTest()
            VerifyRecommendationsAreExactly(
                <ClassDeclaration>Partial |</ClassDeclaration>,
                "Class", "Interface", "MustInherit", "NotInheritable", "Overloads", "Private", "Shadows", "Structure", "Sub")
        End Sub

        <Fact>
        Public Sub KeywordsAfterPartialPrivateTest()
            VerifyRecommendationsAreExactly(
                <ClassDeclaration>Partial Private |</ClassDeclaration>,
                "Class", "Interface", "MustInherit", "NotInheritable", "Overloads", "Shadows", "Structure", "Sub")
        End Sub

#End Region

#Region "Shadows Keyword"

        <Fact>
        Public Sub ShadowsExistsTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterReadOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterWriteOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterNarrowingTest()
            VerifyRecommendationsContain(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterWideningTest()
            VerifyRecommendationsContain(<ClassDeclaration>Widening |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsNotAfterShadowsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shadows |</ClassDeclaration>, "Shadows")
        End Sub

        <Fact>
        Public Sub ShadowsAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Shadows")
        End Sub

#End Region

#Region "Shared Keyword"

        <Fact>
        Public Sub SharedDoesExistTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Shared")
        End Sub

        <Fact>
        Public Sub SharedDoesNotExistAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Shared")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        Public Sub SharedAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Shared")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        Public Sub OverridesDoesNotExistAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Overrides")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545039")>
        Public Sub ShadowsAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Shadows")
        End Sub

#End Region

    End Class
End Namespace
