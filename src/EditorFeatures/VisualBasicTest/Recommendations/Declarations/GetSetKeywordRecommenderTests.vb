' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class GetSetKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAndSetAfterAutoProp()
            VerifyRecommendationsContain(<ClassDeclaration>Property Foo As Integer
|</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAndSetAfterAutoPropAndPrivateAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>Property Foo As Integer
Private |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAndSetAfterAutoPropAndProtectedAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>Property Foo As Integer
Protected |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAndSetAfterAutoPropAndFriendAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>Property Foo As Integer
Friend |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAndSetAfterAutoPropAndProtectedFriendAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>Property Foo As Integer
Protected Friend |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAndSetAfterAutoPropAndFriendProtectedAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>Property Foo As Integer
Friend Protected |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAfterReadOnlyProperty()
            VerifyRecommendationsContain(<ClassDeclaration>Property ReadOnly Foo As Integer
|</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAfterReadOnlyPropertyAndPrivateAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>Property ReadOnly Foo As Integer
Private |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAfterReadOnlyPropertyAndProtectedAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Foo As Integer
Protected |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAfterReadOnlyPropertyAndFriendAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Foo As Integer
Friend |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAfterReadOnlyPropertyAndProtectedFriendAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Foo As Integer
Protected Friend |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetAfterReadOnlyPropertyAndFriendProtectedAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Foo As Integer
Friend Protected |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SetAfterWriteOnlyProperty()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Foo As Integer
|</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SetAfterWriteOnlyPropertyAndPrivateAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Foo As Integer
Private |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SetAfterWriteOnlyPropertyAndProtectedAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Foo As Integer
Protected |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SetAfterWriteOnlyPropertyAndFriendAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Foo As Integer
Friend |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SetAfterWriteOnlyPropertyAndProtectedFriendAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Foo As Integer
Protected Friend |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SetAfterWriteOnlyPropertyAndFriendProtectedAccessor()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Foo As Integer
Friend Protected |</ClassDeclaration>, "Set")
        End Sub
    End Class
End Namespace
