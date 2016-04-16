' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class DelegateKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateInNamespaceTest() As Task
            Await VerifyRecommendationsContainAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<EnumDeclaration>|</EnumDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Partial |</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateAfterPublicInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Public |</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateAfterPublicInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateMissingAfterProtectedInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected |</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateExistsAfterProtectedInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateAfterFriendInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Friend |</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateAfterFriendInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterPrivateInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Private |</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateAfterPrivateInNestedClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterPrivateInNamespaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Namespace Foo
    Private |
End Namespace</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterProtectedFriendInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Protected Friend |</File>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateAfterProtectedFriendInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterOverloadsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Delegate")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DelegateNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Delegate")
        End Function
    End Class
End Namespace
