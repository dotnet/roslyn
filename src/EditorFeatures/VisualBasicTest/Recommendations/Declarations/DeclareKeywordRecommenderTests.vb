' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class DeclareKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub DeclareInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotInInterfaceTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareAfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareAfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Declare")
        End Sub

        <Fact>
        Public Sub DeclareNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Declare")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Declare")
        End Sub
    End Class
End Namespace
