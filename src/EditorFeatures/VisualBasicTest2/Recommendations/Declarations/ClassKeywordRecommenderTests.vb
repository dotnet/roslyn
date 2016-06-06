' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ClassKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Class")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassFollowsClassTest() As Task
            Dim code =
<File>
Class C1

End Class
|
</File>
            Await VerifyRecommendationsContainAsync(code, "Class")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassPrecedesClassTest() As Task
            Dim code =
<File>
|
Class C1

End Class
</File>
            Await VerifyRecommendationsContainAsync(code, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassFollowsDelegateDeclarationTest() As Task
            Dim code =
<File>
Delegate Sub DelegateType()
|
</File>
            Await VerifyRecommendationsContainAsync(code, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassInNamespaceTest() As Task
            Await VerifyRecommendationsContainAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<EnumDeclaration>|</EnumDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Partial |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterPublicInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Public |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterPublicInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassMissingAfterProtectedInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassExistsAfterProtectedInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterFriendInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Friend |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterFriendInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterPrivateInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Private |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterPrivateInNestedClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterPrivateInNamespaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Namespace Foo
    Private |
End Namespace</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterProtectedFriendInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected Friend |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterProtectedFriendInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterMustInheritTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassAfterNotInheritableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Class")
        End Function

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterAsyncTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Class")
        End Function
    End Class
End Namespace
