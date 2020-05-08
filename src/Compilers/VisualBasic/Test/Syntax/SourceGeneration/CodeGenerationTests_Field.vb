' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestField1()
            AssertEx.AreEqual(
"Dim f As Integer",
Field(
    Int32,
    "f").GenerateString())
        End Sub

        <Fact>
        Public Sub TestFieldWithAccessibility1()
            AssertEx.AreEqual(
"Public f As Integer",
Field(
    Int32,
    "f",
    declaredAccessibility:=Accessibility.Public).GenerateString())
        End Sub

        <Fact>
        Public Sub TestFieldWithModifiers1()
            AssertEx.AreEqual(
"Shared f As Integer",
Field(
    Int32,
    "f",
    modifiers:=SymbolModifiers.Static).GenerateString())
        End Sub

        <Fact>
        Public Sub TestFieldWithAccessibilityAndModifiers1()
            AssertEx.AreEqual(
"Private Shared f As Integer",
Field(
    Int32,
    "f",
    declaredAccessibility:=Accessibility.Private,
    modifiers:=SymbolModifiers.Static).GenerateString())
        End Sub

        <Fact>
        Public Sub TestConstantField1()
            AssertEx.AreEqual(
"Const f As Integer",
Field(
    Int32,
    "f",
    modifiers:=SymbolModifiers.Const).GenerateString())
        End Sub
    End Class
End Namespace
