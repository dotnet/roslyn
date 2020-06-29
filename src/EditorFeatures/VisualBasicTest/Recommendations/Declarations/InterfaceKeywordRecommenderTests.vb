' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InterfaceKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceInNamespaceTest() As Task
            Await VerifyRecommendationsContainAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, "Interface")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceInNamespaceFollowsTypeDeclarationTest() As Task
            Dim code =
<File>
Namespace N1
    Class C1

    End Class
    |
End Namespace
</File>
            Await VerifyRecommendationsContainAsync(code, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Interface")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceFollowsInterfaceTest() As Task
            Dim code =
<File>
Interface I1
End Interface
|
</File>
            Await VerifyRecommendationsContainAsync(code, "Interface")
        End Function

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceFollowsMismatchedEndTest() As Task
            Dim code =
<File>
Interface I1
End Class
|
End Interface
</File>
            Await VerifyRecommendationsContainAsync(code, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<EnumDeclaration>|</EnumDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Partial |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterPublicInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Public |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterPublicInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceMissingAfterProtectedInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceExistsAfterProtectedInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterFriendInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Friend |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterFriendInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterPrivateInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Private |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterPrivateInNestedClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterPrivateInNamespaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Namespace Goo
    Private |
End Namespace</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterProtectedFriendInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected Friend |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterProtectedFriendInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InterfaceNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Interface")
        End Function

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterAsyncTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Interface")
        End Function

        <WorkItem(20837, "https://github.com/dotnet/roslyn/issues/20837")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterAttribute() As Task
            Await VerifyRecommendationsContainAsync(<File>&lt;AttributeApplication&gt; |</File>, "Interface")
        End Function
    End Class
End Namespace
