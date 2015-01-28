' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EventKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventInStructureDeclaration()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Event")
        End Sub

        <Fact>
        <WorkItem(544999)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventInInterfaceDeclaration()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterPartial()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterPublic()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterProtected()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterFriend()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterPrivate()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterProtectedFriend()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterOverloads()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterNotOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterMustOverride()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterMustOverrideOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterNotOverridableOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterMustInherit()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterNotInheritable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterCustom()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Custom |</ClassDeclaration>, {"As", "Event"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>dim x = Custom |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterShared()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EventAfterShadows()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Event")
        End Sub

        <WorkItem(674791)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Event")
        End Sub
    End Class
End Namespace
