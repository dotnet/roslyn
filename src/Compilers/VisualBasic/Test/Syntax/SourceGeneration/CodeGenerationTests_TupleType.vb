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
        Public Sub TestTupleWithoutFieldNames()
            AssertEx.AreEqual(
"(Integer, Boolean)",
TupleType(ImmutableArray.Create(
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Int32)),
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Boolean)))).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestTupleWithFirstFieldName()
            AssertEx.AreEqual(
"(a As Integer, Boolean)",
TupleType(ImmutableArray.Create(
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Int32), "a"),
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Boolean)))).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestTupleWithSecondFieldName()
            AssertEx.AreEqual(
"(Integer, b As Boolean)",
TupleType(ImmutableArray.Create(
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Int32)),
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Boolean), "b"))).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestTupleWithFieldNames()
            AssertEx.AreEqual(
"(a As Integer, b As Boolean)",
TupleType(ImmutableArray.Create(
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Int32), "a"),
    TupleElement(CodeGenerator.SpecialType(SpecialType.System_Boolean), "b"))).GenerateTypeString())
        End Sub
    End Class
End Namespace
