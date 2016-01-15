' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideInterfaceDeclaration

#Region "Scope Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PublicDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Public")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PrivateDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Private")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FriendDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Friend")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ProtectedFriendDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Protected Friend")
        End Function

#End Region

#Region "Narrowing and Widening Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NarrowingDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Narrowing")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WideningDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Widening")
        End Function

#End Region

#Region "MustInherit and NotInheritable Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustInheritDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "MustInherit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInheritableDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "NotInheritable")
        End Function

#End Region

#Region "Overrides and Overridable Set of Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridesDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Overrides")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MustOverrideDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "MustOverride")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverridableDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Overridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotOverridableDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "NotOverridable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OverloadsDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Overloads")
        End Function

#End Region

#Region "ReadOnly and WriteOnly Keywords"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReadOnlyDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WriteOnlyDoesExistTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "WriteOnly")
        End Function

#End Region

#Region "Partial Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PartialDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Partial")
        End Function

#End Region

#Region "Shadows Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ShadowsDoesExistTest() As Task
            ' This is actually allowed by the spec. Be careful: the MSDN documentation is wrong
            ' here.
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Shadows")
        End Function

#End Region

#Region "Shared Keyword"

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SharedDoesNotExistTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Shared")
        End Function

#End Region

        <WorkItem(674791)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Friend", "Private", "Protected", "Protected Friend", "Shadows", "Shared",
        "ReadOnly", "WriteOnly", "MustOverride", "Overridable", "Public", "Overloads", "Overrides", "WithEvents")
        End Function
    End Class
End Namespace
