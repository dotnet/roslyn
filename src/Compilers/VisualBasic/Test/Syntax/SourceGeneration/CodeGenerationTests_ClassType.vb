' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestClassType1()
            AssertEx.AreEqual(
"x",
[Class]("x").GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestClassTypeWithTypeArguments1()
            AssertEx.AreEqual(
"X(Of Integer)",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Int32)).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestClassTypeWithTypeArguments2()
            AssertEx.AreEqual(
"X(Of Integer, Boolean)",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Int32, [Boolean])).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestClassDeclaration1()
            AssertEx.AreEqual(
"Class X
End Class",
[Class]("X").GenerateString())
        End Sub

        <Fact>
        Public Sub TestClassDeclarationWithField1()
            AssertEx.AreEqual(
"Class X

    Dim i As Integer
End Class",
[Class](
    "X",
    members:=ImmutableArray.Create(Of ISymbol)(Field(Int32, "i"))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestClassDeclarationWithNestedClass1()
            AssertEx.AreEqual(
"Class X

    Class Y
    End Class
End Class",
[Class](
    "X",
    members:=ImmutableArray.Create(Of ISymbol)([Class]("Y"))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestClassWithBaseType()
            AssertEx.AreEqual(
"Class X
    Inherits Y

End Class",
[Class](
    "X",
    baseType:=[Class]("Y")).GenerateString())
        End Sub

        <Fact>
        Public Sub TestClassWithInterfaces()
            AssertEx.AreEqual(
"Class X
    Implements Y
    Implements Z

End Class",
[Class](
    "X",
    interfaces:=ImmutableArray.Create([Class]("Y"), [Class]("Z"))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClass()
            AssertEx.AreEqual(
"Class X(Of Y)
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(TypeParameter("Y"))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithInVariance()
            AssertEx.AreEqual(
"Class X(Of In Y)
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            variance:=VarianceKind.In))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithOutVariance()
            AssertEx.AreEqual(
"Class X(Of Out Y)
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            variance:=VarianceKind.Out))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithConstructorConstraint()
            AssertEx.AreEqual(
"Class X(Of Y As {New})
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            hasConstructorConstraint:=True))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithNotNullConstraint()
            AssertEx.AreEqual(
"Class X(Of Y)
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            hasNotNullConstraint:=True))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithReferenceTypeConstraint()
            AssertEx.AreEqual(
"Class X(Of Y As {Class})
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            hasReferenceTypeConstraint:=True))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithUnmanagedConstraint()
            AssertEx.AreEqual(
"Class X(Of Y)
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            hasUnmanagedTypeConstraint:=True))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithValueConstraint()
            AssertEx.AreEqual(
"Class X(Of Y As {Structure})
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            hasValueTypeConstraint:=True))).GenerateString())
        End Sub

        <Fact>
        Public Sub TestGenericClassWithTypeConstraint()
            AssertEx.AreEqual(
"Class X(Of Y As {Z})
End Class",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Of ITypeSymbol)(
        TypeParameter(
            "Y",
            constraintTypes:=ImmutableArray.Create(Of ITypeSymbol)(
                [Class]("Z"))))).GenerateString())
        End sub
    End Class
End Namespace
