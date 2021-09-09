' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class StructureKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Structure")
        End Sub

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureFollowsStructureTest()
            Dim code =
<File>
Structure S
End Structure
|
</File>
            VerifyRecommendationsContain(code, "Structure")
        End Sub

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Structure")
        End Sub

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterPartialTest()
            VerifyRecommendationsContain(<File>Partial |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterPublicInFileTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterPublicInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureMissingAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureExistsAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterFriendInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterPrivateInNestedClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<File>
Namespace Goo
    Private |
End Namespace</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureAfterProtectedFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StructureNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Structure")
        End Sub

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterAsyncTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Structure")
        End Sub

        <WorkItem(20837, "https://github.com/dotnet/roslyn/issues/20837")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterAttribute()
            VerifyRecommendationsContain(<File>&lt;AttributeApplication&gt; |</File>, "Structure")
        End Sub
    End Class
End Namespace
