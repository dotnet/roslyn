﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class IteratorKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeywordsAfterIteratorTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Iterator |</ClassDeclaration>,
                                            "Friend", "Function", "Private", "Property", "Protected", "Protected Friend", "Public")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInInterfaceTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterFriendProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend Protected |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterDimTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWithEventsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WithEvents |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterFunctionTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Function |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterSubTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterPropertyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Property |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterReadOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterAsyncTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterDeclareTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Class |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEnumTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Enum |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterInterfaceTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Interface |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterStructureTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Structure |</ClassDeclaration>, "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWriteOnlySharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly Shared |</ClassDeclaration>, "Iterator")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Iterator")
        End Sub
    End Class
End Namespace
