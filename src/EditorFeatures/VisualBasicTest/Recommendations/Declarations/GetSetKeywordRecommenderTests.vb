' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GetSetKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub GetAndSetAfterAutoPropTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property Goo As Integer
|</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        Public Sub GetAndSetAfterAutoPropAndPrivateAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property Goo As Integer
Private |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        Public Sub GetAndSetAfterAutoPropAndProtectedAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property Goo As Integer
Protected |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        Public Sub GetAndSetAfterAutoPropAndFriendAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property Goo As Integer
Friend |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        Public Sub GetAndSetAfterAutoPropAndProtectedFriendAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property Goo As Integer
Protected Friend |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        Public Sub GetAndSetAfterAutoPropAndFriendProtectedAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property Goo As Integer
Friend Protected |</ClassDeclaration>, "Get", "Set")
        End Sub

        <Fact>
        Public Sub GetAfterReadOnlyPropertyTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property ReadOnly Goo As Integer
|</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        Public Sub GetAfterReadOnlyPropertyAndPrivateAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Property ReadOnly Goo As Integer
Private |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        Public Sub GetAfterReadOnlyPropertyAndProtectedAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Goo As Integer
Protected |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        Public Sub GetAfterReadOnlyPropertyAndFriendAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Goo As Integer
Friend |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        Public Sub GetAfterReadOnlyPropertyAndProtectedFriendAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Goo As Integer
Protected Friend |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        Public Sub GetAfterReadOnlyPropertyAndFriendProtectedAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly Property Goo As Integer
Friend Protected |</ClassDeclaration>, "Get")
        End Sub

        <Fact>
        Public Sub SetAfterWriteOnlyPropertyTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Goo As Integer
|</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        Public Sub SetAfterWriteOnlyPropertyAndPrivateAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Goo As Integer
Private |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        Public Sub SetAfterWriteOnlyPropertyAndProtectedAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Goo As Integer
Protected |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        Public Sub SetAfterWriteOnlyPropertyAndFriendAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Goo As Integer
Friend |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        Public Sub SetAfterWriteOnlyPropertyAndProtectedFriendAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Goo As Integer
Protected Friend |</ClassDeclaration>, "Set")
        End Sub

        <Fact>
        Public Sub SetAfterWriteOnlyPropertyAndFriendProtectedAccessorTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly Property Goo As Integer
Friend Protected |</ClassDeclaration>, "Set")
        End Sub
    End Class
End Namespace
