' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideStructureDeclaration

#Region "Scope Keywords"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Public")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedMissing()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Protected")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Private")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Friend")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendMissing()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Protected Friend")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicNotAfterPublic()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Public")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterPublic()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Protected")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateNotAfterPublic()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Private")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterPublic()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Friend")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendNotAfterPublic()
            VerifyRecommendationsMissing(<StructureDeclaration>Public |</StructureDeclaration>, "Protected Friend")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterProtected()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Friend")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendNotAfterProtectedFriend()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Friend")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterFriend()
            VerifyRecommendationsMissing(<StructureDeclaration>Friend |</StructureDeclaration>, "Protected")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedNotAfterProtectedFriend()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Protected")
        End Sub

#End Region

#Region "Narrowing and Widening Keywords"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterWidening()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNarrowing()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterProtected()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterProtected()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterPrivate()
            VerifyRecommendationsMissing(<StructureDeclaration>Private |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterPrivate()
            VerifyRecommendationsMissing(<StructureDeclaration>Private |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterProtectedFriend()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterProtectedFriend()
            VerifyRecommendationsMissing(<StructureDeclaration>Protected Friend |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterMustInherit()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNotInheritable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterNotInheritable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterMustInherit()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingAfterOverloads()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningAfterOverloads()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingNotAfterPartial()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningNotAfterPartial()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Widening")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingAfterShared()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "Narrowing")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningAfterShared()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "Widening")
        End Sub

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNotInheritable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterMustInherit()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNarrowing()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterNarrowing()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterWidening()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterWidening()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritAfterPartial()
            VerifyRecommendationsContain(<StructureDeclaration>Partial |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableAfterPartial()
            VerifyRecommendationsContain(<StructureDeclaration>Partial |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterReadOnly()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterReadOnly()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "NotInheritable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritNotAfterShared()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "MustInherit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableNotAfterShared()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "NotInheritable")
        End Sub

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Overrides")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterOverrides()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overrides")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overrides")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overrides")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overrides")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overrides")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesAfterOverloads()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overrides")
        End Sub

        ' ---------

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideMissing()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "MustOverride")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverrides()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "MustOverride")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "MustOverride")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "MustOverride")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "MustOverride")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "MustOverride")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideNotAfterOverloads()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "MustOverride")
        End Sub

        ' ---------

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableMissing()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Overridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverrides()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableNotAfterOverloads()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overridable")
        End Sub

        ' ---------

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableMissing()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "NotOverridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableAfterOverrides()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "NotOverridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "NotOverridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "NotOverridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "NotOverridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "NotOverridable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableNotAfterOverloads()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "NotOverridable")
        End Sub

        ' ---------

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Overloads")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsAfterOverrides()
            VerifyRecommendationsContain(<StructureDeclaration>Overrides |</StructureDeclaration>, "Overloads")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Overloads")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Overloads")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Overloads")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Overloads")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsNotAfterOverloads()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Overloads")
        End Sub

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterShared()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterShared()
            VerifyRecommendationsContain(<StructureDeclaration>Shared |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterDefault()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterDefault()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterMustInherit()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterMustInherit()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyAfterOverloads()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyAfterOverloads()
            VerifyRecommendationsContain(<StructureDeclaration>Overloads |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterNarrowing()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterNarrowing()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "WriteOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyNotAfterPartial()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyNotAfterPartial()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "WriteOnly")
        End Sub

#End Region

#Region "Partial Keyword"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterPartial()
            VerifyRecommendationsMissing(<StructureDeclaration>Partial |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterMustInherit()
            VerifyRecommendationsContain(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialAfterNotInheritable()
            VerifyRecommendationsContain(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverloads()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverrides()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterReadOnly()
            VerifyRecommendationsMissing(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterWriteOnly()
            VerifyRecommendationsMissing(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterNarrowing()
            VerifyRecommendationsMissing(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterWidening()
            VerifyRecommendationsMissing(<StructureDeclaration>Widening |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Partial")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialNotAfterDefault()
            VerifyRecommendationsMissing(<StructureDeclaration>Default |</StructureDeclaration>, "Partial")
        End Sub

#End Region

#Region "Shadows Keyword"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsExists()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterMustOverride()
            VerifyRecommendationsMissing(<StructureDeclaration>MustOverride |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterMustInherit()
            VerifyRecommendationsMissing(<StructureDeclaration>MustInherit |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterNotInheritable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotInheritable |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterNotOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>NotOverridable |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverloads()
            VerifyRecommendationsMissing(<StructureDeclaration>Overloads |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverrides()
            VerifyRecommendationsMissing(<StructureDeclaration>Overrides |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterOverridable()
            VerifyRecommendationsMissing(<StructureDeclaration>Overridable |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterReadOnly()
            VerifyRecommendationsContain(<StructureDeclaration>ReadOnly |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterWriteOnly()
            VerifyRecommendationsContain(<StructureDeclaration>WriteOnly |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterNarrowing()
            VerifyRecommendationsContain(<StructureDeclaration>Narrowing |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterWidening()
            VerifyRecommendationsContain(<StructureDeclaration>Widening |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsNotAfterShadows()
            VerifyRecommendationsMissing(<StructureDeclaration>Shadows |</StructureDeclaration>, "Shadows")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsAfterDefault()
            VerifyRecommendationsContain(<StructureDeclaration>Default |</StructureDeclaration>, "Shadows")
        End Sub

#End Region

#Region "Shared Keyword"

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesExist()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Shared")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesNotExistAfterShared()
            VerifyRecommendationsMissing(<StructureDeclaration>Shared |</StructureDeclaration>, "Shared")
        End Sub

#End Region

    End Class
End Namespace
