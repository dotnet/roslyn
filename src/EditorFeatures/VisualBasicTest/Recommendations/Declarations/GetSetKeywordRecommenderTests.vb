' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class GetSetKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAndSetAfterAutoPropTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property Foo As Integer
|</ClassDeclaration>, "Get", "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAndSetAfterAutoPropAndPrivateAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property Foo As Integer
Private |</ClassDeclaration>, "Get", "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAndSetAfterAutoPropAndProtectedAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property Foo As Integer
Protected |</ClassDeclaration>, "Get", "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAndSetAfterAutoPropAndFriendAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property Foo As Integer
Friend |</ClassDeclaration>, "Get", "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAndSetAfterAutoPropAndProtectedFriendAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property Foo As Integer
Protected Friend |</ClassDeclaration>, "Get", "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAndSetAfterAutoPropAndFriendProtectedAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property Foo As Integer
Friend Protected |</ClassDeclaration>, "Get", "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAfterReadOnlyPropertyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property ReadOnly Foo As Integer
|</ClassDeclaration>, "Get")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAfterReadOnlyPropertyAndPrivateAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property ReadOnly Foo As Integer
Private |</ClassDeclaration>, "Get")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAfterReadOnlyPropertyAndProtectedAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly Property Foo As Integer
Protected |</ClassDeclaration>, "Get")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAfterReadOnlyPropertyAndFriendAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly Property Foo As Integer
Friend |</ClassDeclaration>, "Get")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAfterReadOnlyPropertyAndProtectedFriendAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly Property Foo As Integer
Protected Friend |</ClassDeclaration>, "Get")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function GetAfterReadOnlyPropertyAndFriendProtectedAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly Property Foo As Integer
Friend Protected |</ClassDeclaration>, "Get")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SetAfterWriteOnlyPropertyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly Property Foo As Integer
|</ClassDeclaration>, "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SetAfterWriteOnlyPropertyAndPrivateAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly Property Foo As Integer
Private |</ClassDeclaration>, "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SetAfterWriteOnlyPropertyAndProtectedAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly Property Foo As Integer
Protected |</ClassDeclaration>, "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SetAfterWriteOnlyPropertyAndFriendAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly Property Foo As Integer
Friend |</ClassDeclaration>, "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SetAfterWriteOnlyPropertyAndProtectedFriendAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly Property Foo As Integer
Protected Friend |</ClassDeclaration>, "Set")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SetAfterWriteOnlyPropertyAndFriendProtectedAccessorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly Property Foo As Integer
Friend Protected |</ClassDeclaration>, "Set")
        End Function
    End Class
End Namespace
