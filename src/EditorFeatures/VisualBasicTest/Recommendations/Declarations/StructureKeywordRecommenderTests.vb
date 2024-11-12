' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class StructureKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub StructureInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Structure")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub StructureFollowsStructureTest()
            Dim code =
<File>
Structure S
End Structure
|
</File>
            VerifyRecommendationsContain(code, "Structure")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub StructureFollowsStructureWithinClassTest()
            Dim code =
<File>
Class C
    Structure S
    End Structure
    |
End Class
</File>
            VerifyRecommendationsContain(code, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Structure")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub StructureFollowsMismatchedEndTest()
            Dim code =
<File>
Interface I1
End Class
|
End Interface
</File>
            VerifyRecommendationsContain(code, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterPartialTest()
            VerifyRecommendationsContain(<File>Partial |</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterPublicInFileTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterPublicInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureMissingAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureExistsAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterFriendInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterPrivateInNestedClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<File>
Namespace Goo
    Private |
End Namespace</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureAfterProtectedFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        Public Sub StructureNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Structure")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        Public Sub NotAfterAsyncTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Structure")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20837")>
        Public Sub AfterAttribute()
            VerifyRecommendationsContain(<File>&lt;AttributeApplication&gt; |</File>, "Structure")
        End Sub
    End Class
End Namespace
