' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class DelegateKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub DelegateInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterPartialTest()
            VerifyRecommendationsMissing(<File>Partial |</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateAfterPublicInFileTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateAfterPublicInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateMissingAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateExistsAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateAfterFriendInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateAfterPrivateInNestedClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<File>
Namespace Goo
    Private |
End Namespace</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateAfterProtectedFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Delegate")
        End Sub

        <Fact>
        Public Sub DelegateNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Delegate")
        End Sub
    End Class
End Namespace
