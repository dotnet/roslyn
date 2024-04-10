' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ModuleKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ModuleInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotInInterfaceTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotInStructureTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleAfterPartialTest()
            VerifyRecommendationsContain(<File>Partial |</File>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleAfterPublicTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Module")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub ModuleAfterEndModuleTest()
            Dim code =
<File>
Module M1

End Module
|
</File>
            VerifyRecommendationsContain(code, "Module")
        End Sub

        <Fact>
        Public Sub ModuleFollowsDelegateDeclarationTest()
            Dim code =
<File>
Delegate Sub DelegateType()
|
</File>
            VerifyRecommendationsContain(code, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterPublicInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterFriendInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Friend |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterPrivateInNestedClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>Private |</NamespaceDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterProtectedFriendInClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        Public Sub ModuleNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Module")
        End Sub
    End Class
End Namespace
