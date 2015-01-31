' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class OfKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfAfterPossibleMethodTypeParam()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotAfterMethodTypeParam()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo(Of T)(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfDefinitelyInMethodTypeParam()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo(|)(x As Integer)</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfAfterPossibleDelegateTypeParam()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub Foo(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotAfterDelegateTypeParam()
            VerifyRecommendationsMissing(<ClassDeclaration>Delegate Sub Foo(Of T)(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfDefinitelyInDelegateTypeParam()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub Foo(|)(x As Integer)</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInClassDeclarationTypeParam()
            VerifyRecommendationsContain(<File>Class Foo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInStructureDeclarationTypeParam()
            VerifyRecommendationsContain(<File>Structure Foo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInInterfaceDeclarationTypeParam()
            VerifyRecommendationsContain(<File>Interface Foo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotInEnumDeclaration()
            ' This is invalid code, so make sure we don't show it
            VerifyRecommendationsMissing(<File>Enum Foo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotInModuleDeclaration()
            VerifyRecommendationsMissing(<File>Module Foo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInVariableDeclaration1()
            VerifyRecommendationsMissing(<MethodBody>Dim f As Foo(|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInVariableDeclaration2()
            VerifyRecommendationsContain(<MethodBody>Dim f As New Foo(|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotInRealArraySpecifier()
            VerifyRecommendationsMissing(<MethodBody>Dim f(|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInMethodCall()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "Of")
        End Sub

        <WorkItem(541636)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInGenericArrayBoundRankSpecifier()
            VerifyRecommendationsContain(<MethodBody>Dim i As List(|</MethodBody>, "Of")
        End Sub

        <WorkItem(541636)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoOfInNonGenericArrayBoundRankSpecifier()
            VerifyRecommendationsMissing(<MethodBody>Dim i As Integer(|</MethodBody>, "Of")
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInNonGenericDelegateCreation()
            Dim code =
<File>
Class C
    Delegate Sub Foo()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
</File>

            VerifyRecommendationsMissing(code, "Of")
        End Sub

        <WorkItem(529552)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InGenericDelegateCreation()
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Delegate Sub Foo(Of C)()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "Of")
        End Sub

        <WorkItem(529552)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InPotentiallyGenericDelegateCreation()
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Delegate Sub Foo()
    Delegate Sub Foo(Of C)()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "Of")
        End Sub

        <WorkItem(529552)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInNonGenericDelegateCreationWithGenericTypeOfSameName()
            Dim code =
<File>
Class Foo(Of U)
End Class
Class C
    Delegate Sub Foo()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
</File>

            VerifyRecommendationsMissing(code, "Of")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterEol()
            VerifyRecommendationsContain(
<MethodBody>Foo(
|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InImplementsClause()
            Dim code =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class G(Of U)
    Implements IEquatable(Of U)

    Public Function Equals(other As U) As Boolean Implements IEquatable(|
        Throw New NotImplementedException()
    End Function
End Class
</File>

            VerifyRecommendationsContain(code, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInheritsStatement()
            Dim code =
<File>
Class G(Of T)
End Class

Class DG
    Inherits G(|
End Class
</File>

            VerifyRecommendationsContain(code, "Of")
        End Sub
    End Class
End Namespace
