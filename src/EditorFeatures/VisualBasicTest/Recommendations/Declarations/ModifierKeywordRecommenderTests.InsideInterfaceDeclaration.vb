' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideInterfaceDeclaration

#Region "Scope Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PublicDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Protected")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PrivateDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Private")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FriendDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Friend")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ProtectedFriendDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Protected Friend")
        End Sub

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NarrowingDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Narrowing")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WideningDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Widening")
        End Sub

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustInheritDoesExist()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "MustInherit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInheritableDoesExist()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "NotInheritable")
        End Sub

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridesDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Overrides")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MustOverrideDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "MustOverride")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverridableDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Overridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotOverridableDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "NotOverridable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OverloadsDoesExist()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Overloads")
        End Sub

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReadOnlyDoesExist()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "ReadOnly")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WriteOnlyDoesExist()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "WriteOnly")
        End Sub

#End Region

#Region "Partial Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PartialDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Partial")
        End Sub

#End Region

#Region "Shadows Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ShadowsDoesExist()
            ' This is actually allowed by the spec. Be careful: the MSDN documentation is wrong
            ' here.
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Shadows")
        End Sub

#End Region

#Region "Shared Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SharedDoesNotExist()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Shared")
        End Sub

#End Region

        <WorkItem(674791)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Friend", "Private", "Protected", "Protected Friend", "Shadows", "Shared",
        "ReadOnly", "WriteOnly", "MustOverride", "Overridable", "Public", "Overloads", "Overrides", "WithEvents")
        End Sub

    End Class
End Namespace
