' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class OfKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfAfterPossibleMethodTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(|</ClassDeclaration>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfNotAfterMethodTypeParamTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo(Of T)(|</ClassDeclaration>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfDefinitelyInMethodTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo(|)(x As Integer)</ClassDeclaration>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfAfterPossibleDelegateTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Sub Foo(|</ClassDeclaration>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfNotAfterDelegateTypeParamTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Delegate Sub Foo(Of T)(|</ClassDeclaration>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfDefinitelyInDelegateTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Delegate Sub Foo(|)(x As Integer)</ClassDeclaration>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInClassDeclarationTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo(|</File>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInStructureDeclarationTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Structure Foo(|</File>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInInterfaceDeclarationTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface Foo(|</File>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfNotInEnumDeclarationTest() As Task
            ' This is invalid code, so make sure we don't show it
            Await VerifyRecommendationsMissingAsync(<File>Enum Foo(|</File>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfNotInModuleDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Module Foo(|</File>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInVariableDeclaration1Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim f As Foo(|</MethodBody>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInVariableDeclaration2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim f As New Foo(|</MethodBody>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfNotInRealArraySpecifierTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim f(|</MethodBody>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInMethodCallTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(|</MethodBody>, "Of")
        End Function

        <WorkItem(541636)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OfInGenericArrayBoundRankSpecifierTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim i As List(|</MethodBody>, "Of")
        End Function

        <WorkItem(541636)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoOfInNonGenericArrayBoundRankSpecifierTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim i As Integer(|</MethodBody>, "Of")
        End Function

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInNonGenericDelegateCreationTest() As Task
            Dim code =
<File>
Class C
    Delegate Sub Foo()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
</File>

            Await VerifyRecommendationsMissingAsync(code, "Of")
        End Function

        <WorkItem(529552)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InGenericDelegateCreationTest() As Task
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Delegate Sub Foo(Of C)()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
]]></ModuleDeclaration>
            Await VerifyRecommendationsContainAsync(code, "Of")
        End Function

        <WorkItem(529552)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InPotentiallyGenericDelegateCreationTest() As Task
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Delegate Sub Foo()
    Delegate Sub Foo(Of C)()

    Sub Main(args As String())
        Dim f1 As New Foo(|
    End Sub
End Class
]]></ModuleDeclaration>
            Await VerifyRecommendationsContainAsync(code, "Of")
        End Function

        <WorkItem(529552)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInNonGenericDelegateCreationWithGenericTypeOfSameNameTest() As Task
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

            Await VerifyRecommendationsMissingAsync(code, "Of")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterEolTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>Foo(
|</MethodBody>, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InImplementsClauseTest() As Task
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

            Await VerifyRecommendationsContainAsync(code, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InInheritsStatementTest() As Task
            Dim code =
<File>
Class G(Of T)
End Class

Class DG
    Inherits G(|
End Class
</File>

            Await VerifyRecommendationsContainAsync(code, "Of")
        End Function
    End Class
End Namespace
