' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class DeclareKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotInNamespaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotInInterfaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<EnumDeclaration>|</EnumDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareAfterPublicTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareAfterProtectedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareAfterFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareAfterPrivateTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterNotOverridableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterMustOverrideTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Declare")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DeclareNotAfterSharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Declare")
        End Function

        <WorkItem(674791)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Declare")
        End Function
    End Class
End Namespace
