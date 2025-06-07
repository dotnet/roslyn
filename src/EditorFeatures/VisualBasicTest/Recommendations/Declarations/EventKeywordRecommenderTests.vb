' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class EventKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub EventInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventInStructureDeclarationTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Event")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544999")>
        Public Sub EventInInterfaceDeclarationTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterCustomTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Custom |</ClassDeclaration>, {"As", "Event"})
        End Sub

        <Fact>
        Public Sub EventNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>dim x = Custom |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Event")
        End Sub

        <Fact>
        Public Sub EventAfterShadowsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shadows |</ClassDeclaration>, "Event")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Event")
        End Sub
    End Class
End Namespace
