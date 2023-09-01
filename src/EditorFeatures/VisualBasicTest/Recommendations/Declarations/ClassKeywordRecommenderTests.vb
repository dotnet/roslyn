' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ClassKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ClassInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Class")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub ClassFollowsClassTest()
            Dim code =
<File>
Class C1

End Class
|
</File>
            VerifyRecommendationsContain(code, "Class")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        Public Sub ClassPrecedesClassTest()
            Dim code =
<File>
|
Class C1

End Class
</File>
            VerifyRecommendationsContain(code, "Class")
        End Sub

        <Fact>
        Public Sub ClassFollowsDelegateDeclarationTest()
            Dim code =
<File>
Delegate Sub DelegateType()
|
</File>
            VerifyRecommendationsContain(code, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Class")
        End Sub

        <Fact>
        Public Sub ClassInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterPartialTest()
            VerifyRecommendationsContain(<File>Partial |</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterPublicInFileTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterPublicInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassMissingAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassExistsAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterFriendInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterPrivateInNestedClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<File>
Namespace Goo
    Private |
End Namespace</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterProtectedFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterMustInheritTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassAfterNotInheritableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        Public Sub ClassNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Class")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        Public Sub AfterAsyncTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Class")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20837")>
        Public Sub AfterAttribute()
            VerifyRecommendationsContain(<File>&lt;AttributeApplication&gt; |</File>, "Class")
        End Sub
    End Class
End Namespace
