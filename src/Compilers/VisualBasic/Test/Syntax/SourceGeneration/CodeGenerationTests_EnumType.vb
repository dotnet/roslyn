' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestEnumType1()
            AssertEx.AreEqual(
"x",
[Enum]("x").GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestEnumTypeInNamespace1()
            AssertEx.AreEqual(
"NS.x",
[Enum](
    "x",
    containingSymbol:=[Namespace]("NS")).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestEnumTypeInGlobalNamespace1()
            AssertEx.AreEqual(
"Global.x",
[Enum](
    "x",
    containingSymbol:=GlobalNamespace()).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestEnumTypeInNamespaceInGlobal1()
            AssertEx.AreEqual(
"Global.NS.x",
[Enum](
    "x",
    containingSymbol:=[Namespace](
        "NS",
        containingSymbol:=GlobalNamespace())).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestEnumDeclaration1()
            AssertEx.AreEqual(
"Enum X
End Enum",
[Enum]("X").GenerateString())
        End Sub

        <Fact>
        Public Sub TestPublicEnumDeclaration1()
            AssertEx.AreEqual(
"Public Enum X
End Enum",
[Enum](
    "X",
    declaredAccessibility:=Accessibility.Public).GenerateString())
        End Sub

        <Fact>
        Public Sub TestEnumDeclarationInNamespace1()
            AssertEx.AreEqual(
"Namespace N

    Enum X
    End Enum
End Namespace",
[Namespace]("N").WithMembers([Enum]("X")).GenerateString())
        End Sub

        <Fact>
        Public Sub TestEnumDeclarationWithUnderlyingType1()
            AssertEx.AreEqual(
"Enum X As Integer
End Enum",
[Enum](
    "X",
    enumUnderlyingType:=System_Int32).GenerateString())
        End Sub

        <fact>
        Public Sub TestEnumDeclarationWithBaseType1()
            AssertEx.AreEqual(
"Enum X
End Enum",
[Enum]("X").WithBaseType(System_Enum).GenerateString())
        End Sub

        <fact>
        public sub TestEnumDeclarationWithOneMember1()
            AssertEx.AreEqual(
"Enum X
    A
End Enum",
[Enum]("X").WithMembers(EnumMember("A")).GenerateString())
        End sub

        <fact>
        public sub TestEnumDeclarationWithTwoMembers1()
            AssertEx.AreEqual(
"Enum X
    A
    B
End Enum",
[Enum]("X").WithMembers(
    EnumMember("A"),
    EnumMember("B")).GenerateString())
        End sub
    End Class
End Namespace
