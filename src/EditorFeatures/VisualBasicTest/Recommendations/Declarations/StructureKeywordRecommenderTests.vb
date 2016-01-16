' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class StructureKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Structure")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureFollowsStructureTest() As Task
            Dim code =
<File>
Structure S
End Structure
|
</File>
            Await VerifyRecommendationsContainAsync(code, "Structure")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureFollowsStructureWithinClassTest() As Task
            Dim code =
<File>
Class C
    Structure S
    End Structure
    |
End Class
</File>
            Await VerifyRecommendationsContainAsync(code, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureInNamespaceTest() As Task
            Await VerifyRecommendationsContainAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Structure")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureFollowsMismatchedEndTest() As Task
            Dim code =
<File>
Interface I1
End Class
|
End Interface
</File>
            Await VerifyRecommendationsContainAsync(code, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<EnumDeclaration>|</EnumDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Partial |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterPublicInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Public |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterPublicInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureMissingAfterProtectedInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureExistsAfterProtectedInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterFriendInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Friend |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterFriendInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterPrivateInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Private |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterPrivateInNestedClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterPrivateInNamespaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Namespace Foo
    Private |
End Namespace</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterProtectedFriendInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected Friend |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureAfterProtectedFriendInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function StructureNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Structure")
        End Function

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterAsyncTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Structure")
        End Function
    End Class
End Namespace
