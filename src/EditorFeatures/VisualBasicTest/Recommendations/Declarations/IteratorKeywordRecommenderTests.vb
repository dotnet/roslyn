' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class IteratorKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeywordsAfterIterator()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Iterator |</ClassDeclaration>,
                                            "Friend", "Function", "Private", "Property", "Protected", "Protected Friend", "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InClass()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInInterface()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InStructure()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterPrivate()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterProtected()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterProtectedFriend()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterFriendProtected()
            VerifyRecommendationsContain(<ClassDeclaration>Friend Protected |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterFriend()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterPublic()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterOverridable()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterShadows()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterMustOverride()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterDim()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWithEvents()
            VerifyRecommendationsMissing(<ClassDeclaration>WithEvents |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterFunction()
            VerifyRecommendationsMissing(<ClassDeclaration>Function |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterSub()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterProperty()
            VerifyRecommendationsMissing(<ClassDeclaration>Property |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterReadOnly()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterShared()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterAsync()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterDeclare()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterDefault()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterPartial()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterClass()
            VerifyRecommendationsMissing(<ClassDeclaration>Class |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEnum()
            VerifyRecommendationsMissing(<ClassDeclaration>Enum |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterInterface()
            VerifyRecommendationsMissing(<ClassDeclaration>Interface |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterStructure()
            VerifyRecommendationsMissing(<ClassDeclaration>Structure |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWriteOnlyShared()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly Shared |</ClassDeclaration>, "Iterator")
        End Sub

        <WorkItem(674791)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Iterator")
        End Sub
    End Class
End Namespace
