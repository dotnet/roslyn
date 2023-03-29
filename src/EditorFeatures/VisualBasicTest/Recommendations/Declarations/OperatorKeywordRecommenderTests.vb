' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class OperatorKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub OperatorInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotInInterfaceTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Operator")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544630")>
        Public Sub OperatorNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterProtectedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Friend |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterPrivateTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterProtectedFriendTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorCTypeAfterNarrowingTest()
            VerifyRecommendationsContain(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Operator CType")
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorCTypeAfterWideningTest()
            VerifyRecommendationsContain(<ClassDeclaration>Widening |</ClassDeclaration>, "Operator CType")
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Operator")
        End Sub

        <Fact>
        Public Sub OperatorAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Operator")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Operator")
        End Sub
    End Class
End Namespace
