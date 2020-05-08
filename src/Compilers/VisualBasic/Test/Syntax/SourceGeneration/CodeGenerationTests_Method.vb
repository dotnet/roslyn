' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestSubMethod1()
            AssertEx.AreEqual(
"Class C

    Sub M()
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(Void, "M"))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestFunctionMethod1()
            AssertEx.AreEqual(
"Class C

    Function M() As Integer
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(Int32, "M"))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestMethodWithAccessibility1()
            AssertEx.AreEqual(
"Class C

    Public Sub M()
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(
            Void,
            "M",
            declaredAccessibility:=Accessibility.Public))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestMethodWithModifiers1()
            AssertEx.AreEqual(
"Class C

    MustOverride Sub M()
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(
            Void,
            "M",
            modifiers:=SymbolModifiers.Abstract))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericMethod1()
            AssertEx.AreEqual(
"Class C

    Sub M(Of X)()
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(
            Void,
            "M",
            typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
                TypeParameter("X"))))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericMethodWithConstraint1()
            AssertEx.AreEqual(
"Class C

    Sub M(Of X As {New})()
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(
            Void,
            "M",
            typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
                TypeParameter(
                    "X",
                    hasConstructorConstraint:=True))))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestMethodWithParameters1()
            AssertEx.AreEqual(
"Class C

    Sub M(i As Integer)
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(
            Void,
            "M",
            parameters:=ImmutableArray.Create(Parameter(
                Int32, "i"))))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestMethodWithParamsParameters1()
            AssertEx.AreEqual(
"Class C

    Sub M(ParamArray i As Integer)
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(
            Void,
            "M",
            parameters:=ImmutableArray.Create(Parameter(
                Int32,
                "i",
                modifiers:=SymbolModifiers.Params))))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestMethodWithExplicitImpl1()
            AssertEx.AreEqual(
"Class C

    Sub M() Implements I.M
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Method(Void, "M",
            explicitInterfaceImplementations:=ImmutableArray.Create(
                Method(Void, "M",
                    containingSymbol:=[Interface]("I")))))).GenerateString())
        End Sub
    End Class
End Namespace
