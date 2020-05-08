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
        <fact>
        Public Sub TestConstructor1()
            AssertEx.AreEqual(
"Class C

    Sub New()
    End Sub
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Constructor())).GenerateString())
        End Sub

        <fact>
        Public Sub TestGenericTypeConstructor1()
            AssertEx.AreEqual(
"Class C(Of T)

    Sub New()
    End Sub
End Class",
[Class](
    "C",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(TypeParameter("T")),
    members:=ImmutableArray.Create(Of ISymbol)(
        Constructor())).GenerateString())
        End Sub

        <fact>
        Public Sub TestConstructorWithAccessibility1()
            AssertEx.AreEqual(
"Class C

    Public Sub New()
    End Sub
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Constructor(
            declaredAccessibility:=Accessibility.Public))).GenerateString())
        End Sub

        <fact>
        Public Sub TestConstructorWithModifiers1()
            AssertEx.AreEqual(
"Class C

    Shared Sub New()
    End Sub
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Constructor(
            modifiers:=SymbolModifiers.Static))).GenerateString())
        End Sub

        <fact>
        Public Sub TestConstructorWithParameters1()
            AssertEx.AreEqual(
"Class C

    Sub New(i As Integer)
    End Sub
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Constructor(
            parameters:=ImmutableArray.Create(Parameter(
                Int32, "i"))))).GenerateString())
        End Sub

        <fact>
        Public Sub TestConstructorWithParamsParameters1()
            AssertEx.AreEqual(
"Class C

    Sub New(ParamArray i As Integer)
    End Sub
End Class",
[Class](
    "C",
    members:=ImmutableArray.Create(Of ISymbol)(
        Constructor(
            parameters:=ImmutableArray.Create(Parameter(
                Int32,
                "i",
                modifiers:=SymbolModifiers.Params))))).GenerateString())
        End Sub
    End Class
End Namespace
