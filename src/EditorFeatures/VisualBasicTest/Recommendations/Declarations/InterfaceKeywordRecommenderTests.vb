' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class InterfaceKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub InterfaceInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Interface")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub InterfaceInNamespaceFollowsTypeDeclarationTest()
            Dim code =
<File>
Namespace N1
    Class C1

    End Class
    |
End Namespace
</File>
            VerifyRecommendationsContain(code, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Interface")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub InterfaceFollowsInterfaceTest()
            Dim code =
<File>
Interface I1
End Interface
|
</File>
            VerifyRecommendationsContain(code, "Interface")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub InterfaceFollowsMismatchedEndTest()
            Dim code =
<File>
Interface I1
End Class
|
End Interface
</File>
            VerifyRecommendationsContain(code, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterPartialTest()
            VerifyRecommendationsContain(<File>Partial |</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterPublicInFileTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterPublicInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceMissingAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceExistsAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterFriendInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterPrivateInNestedClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<File>
Namespace Goo
    Private |
End Namespace</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterProtectedFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        Public Sub InterfaceNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Interface")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        Public Sub NotAfterAsyncTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Interface")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20837")>
        Public Sub AfterAttribute()
            VerifyRecommendationsContain(<File>&lt;AttributeApplication&gt; |</File>, "Interface")
        End Sub
    End Class
End Namespace
