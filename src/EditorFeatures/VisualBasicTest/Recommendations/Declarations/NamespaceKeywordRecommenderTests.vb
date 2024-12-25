' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NamespaceKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NamespaceInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NestedNamespaceFollowsTypeDeclarationTest()
            Dim code =
<File>
Namespace N1
    Class C1

    End Class
    |
End Namespace
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NamespaceFollowsNamespaceTest()
            Dim code =
<File>
Namespace N1
    Class C1

    End Class
End Namespace
|
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NamespaceFollowsNamespaceWithoutMatchingEndTest()
            Dim code =
<File>
Namespace N1
|
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NamespaceFollowsMismatchedEnd1Test()
            Dim code =
<File>
Namespace N1
End Class
|
End Namespace
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NamespaceFollowsMismatchedEnd2Test()
            Dim code =
<File>
Namespace N1
End Class
|
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NamespaceFollowsMismatchedEnd3Test()
            Dim code =
<File>
End Class
|
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub NamespaceFollowsMismatchedEnd4Test()
            Dim code =
<File>
End Class
|
End Namespace
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceFollowsDelegateDeclarationTest()
            Dim code =
<File>
Delegate Sub DelegateType()
|
</File>
            VerifyRecommendationsContain(code, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotInInterfaceTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotInStructureTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterPartialTest()
            VerifyRecommendationsMissing(<File>Partial |</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterPublicTest()
            VerifyRecommendationsMissing(<File>Public |</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterPublicInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterFriendInFileTest()
            VerifyRecommendationsMissing(<File>Friend |</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterFriendInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Friend |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterPrivateInNestedClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>Private |</NamespaceDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterProtectedFriendInClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Namespace")
        End Sub

        <Fact>
        Public Sub NamespaceNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Namespace")
        End Sub
    End Class
End Namespace
